using Castle.DynamicProxy;
using F23Bag.Data.DML;
using System;
using System.Collections.Generic;
using System.Linq;

namespace F23Bag.Data.Mapping
{
    internal class LazyProxyGenerator 
    {
        public static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();
        private readonly DbQueryProvider _queryProvider;
        private readonly List<object> _objectIds;
        private readonly Dictionary<object, object> _loadedObjects;

        public LazyProxyGenerator(DbQueryProvider queryProvider)
        {
            _queryProvider = queryProvider;
            _objectIds = new List<object>();
            _loadedObjects = new Dictionary<object, object>();
        }

        public object GetProxy(LoadingPropertyInfo loadingPropertyInfo, object objectId)
        {
            if (loadingPropertyInfo.LazyLoadingType == LazyLoadingType.Simple)
                return ProxyGenerator.CreateClassProxy(loadingPropertyInfo.Property.PropertyType, new SingleObjectInterceptor(_queryProvider, loadingPropertyInfo, objectId));
            else
            {
                _objectIds.Add(objectId);
                return ProxyGenerator.CreateClassProxy(loadingPropertyInfo.Property.PropertyType, new BatchSingleObjectInterceptor(_queryProvider, loadingPropertyInfo, objectId, _objectIds, _loadedObjects));
            }
        }

        public object GetProxyForCollection(LoadingPropertyInfo loadingPropertyInfo, object parentId)
        {
            if (!loadingPropertyInfo.Property.PropertyType.IsInterface) throw new NotSupportedException("Only interface are supported for collection property : " + loadingPropertyInfo.Property.Name);
            return ProxyGenerator.CreateInterfaceProxyWithoutTarget(loadingPropertyInfo.Property.PropertyType, new CollectionInterceptor(_queryProvider, loadingPropertyInfo, parentId));
        }

        private class SingleObjectInterceptor : IInterceptor
        {
            private readonly DbQueryProvider _queryProvider;
            private readonly LoadingPropertyInfo _loadingPropertyInfo;
            private readonly object _objectId;
            private object _loadedObject;

            public SingleObjectInterceptor(DbQueryProvider queryProvider, LoadingPropertyInfo loadingPropertyInfo, object objectId)
            {
                _queryProvider = queryProvider;
                _loadingPropertyInfo = loadingPropertyInfo;
                _objectId = objectId;
            }

            public void Intercept(IInvocation invocation)
            {
                if (_loadedObject == null)
                {
                    var elementType = _loadingPropertyInfo.Property.PropertyType;
                    var idProperty = _queryProvider.SqlMapping.GetIdProperty(elementType);

                    if (invocation.Method.Name == "get_" + idProperty.Name)
                    {
                        invocation.ReturnValue = Convert.ChangeType(_objectId, invocation.Method.ReturnType);
                        return;
                    }

                    var request = new Request()
                    {
                        RequestType = RequestType.Select,
                        FromAlias = new AliasDefinition(_queryProvider.SqlMapping.GetSqlEquivalent(elementType)),
                        ProjectionType = elementType,
                        Take = 1
                    };
                    request.Where = new BinaryExpression(BinaryExpressionTypeEnum.Equal,
                        new ColumnAccess(request.FromAlias, new Identifier(_queryProvider.SqlMapping.GetColumnName(idProperty))),
                        new Constant(Convert.ChangeType(_objectId, idProperty.PropertyType)));

                    var mapper = new Mapper(_queryProvider);
                    mapper.LoadingPropertyInfos.AddRange(_loadingPropertyInfo.GetSubLoadingPropertyInfoforLazy());
                    mapper.DeclareMap(request, null);

                    _loadedObject = _queryProvider.Execute(mapper, request, elementType);
                }

                invocation.ReturnValue = invocation.Method.Invoke(_loadedObject, invocation.Arguments);
            }
        }

        private class CollectionInterceptor : IInterceptor
        {
            private readonly DbQueryProvider _queryProvider;
            private readonly LoadingPropertyInfo _loadingPropertyInfo;
            private readonly object _parentId;
            private object _collection;

            public CollectionInterceptor(DbQueryProvider queryProvider, LoadingPropertyInfo loadingPropertyInfo, object parentId)
            {
                _queryProvider = queryProvider;
                _loadingPropertyInfo = loadingPropertyInfo;
                _parentId = parentId;
            }

            public void Intercept(IInvocation invocation)
            {
                if (_collection == null)
                {
                    var elementType = _loadingPropertyInfo.Property.PropertyType.GetGenericArguments()[0];

                    var request = new Request()
                    {
                        RequestType = RequestType.Select,
                        FromAlias = new AliasDefinition(_queryProvider.SqlMapping.GetSqlEquivalent(elementType)),
                        ProjectionType = elementType
                    };
                    request.Where = new BinaryExpression(BinaryExpressionTypeEnum.Equal,
                        new ColumnAccess(request.FromAlias, new Identifier(_queryProvider.SqlMapping.GetColumnName(_loadingPropertyInfo.Property))),
                        new Constant(_parentId));

                    var mapper = new Mapper(_queryProvider);
                    mapper.LoadingPropertyInfos.AddRange(_loadingPropertyInfo.GetSubLoadingPropertyInfoforLazy());
                    mapper.DeclareMap(request, null);

                    if (invocation.Method.Name == "get_Count")
                    {
                        request.Select.Clear();
                        request.Select.Add(new SelectInfo(new UnaryExpression(UnaryExpressionTypeEnum.Count, null), null));
                        invocation.ReturnValue = Convert.ChangeType(_queryProvider.Execute(mapper, request, invocation.Method.ReturnType), invocation.Method.ReturnType);
                        return;
                    }

                    _collection = new CollectionActivator().CreateInstance(_loadingPropertyInfo.Property.PropertyType, (System.Collections.IEnumerable)_queryProvider.Execute(mapper, request, _loadingPropertyInfo.Property.PropertyType));
                }

                invocation.ReturnValue = invocation.Method.Invoke(_collection, invocation.Arguments);
            }
        }

        private class BatchSingleObjectInterceptor : IInterceptor
        {
            private readonly DbQueryProvider _queryProvider;
            private readonly LoadingPropertyInfo _loadingPropertyInfo;
            private readonly object _objectId;
            private readonly List<object> _objectIds;
            private readonly Dictionary<object, object> _loadedObjects;
            private object _loadedObject;

            public BatchSingleObjectInterceptor(DbQueryProvider queryProvider, LoadingPropertyInfo loadingPropertyInfo, object objectId, List<object> objectIds, Dictionary<object, object> loadedObjects)
            {
                _queryProvider = queryProvider;
                _loadingPropertyInfo = loadingPropertyInfo;
                _objectId = objectId;
                _objectIds = objectIds;
                _loadedObjects = loadedObjects;
            }

            public void Intercept(IInvocation invocation)
            {
                if (_loadedObject == null)
                {
                    var elementType = _loadingPropertyInfo.Property.PropertyType;
                    var idProperty = _queryProvider.SqlMapping.GetIdProperty(elementType);

                    if (invocation.Method.Name == "get_" + idProperty.Name)
                    {
                        invocation.ReturnValue = Convert.ChangeType(_objectId, invocation.Method.ReturnType);
                        return;
                    }

                    if (!_loadedObjects.ContainsKey(_objectId))
                    {
                        var getId = new PropertyAccessorCompiler(idProperty).GetPropertyValue;

                        var request = new Request()
                        {
                            RequestType = RequestType.Select,
                            FromAlias = new AliasDefinition(_queryProvider.SqlMapping.GetSqlEquivalent(elementType)),
                            ProjectionType = elementType
                        };

                        request.Where = new BinaryExpression(BinaryExpressionTypeEnum.Equal,
                            new ColumnAccess(request.FromAlias, new Identifier(_queryProvider.SqlMapping.GetColumnName(idProperty))),
                            new Constant(Convert.ChangeType(_objectIds.First(), idProperty.PropertyType)));

                        foreach (var id in _objectIds.Skip(1))
                            request.Where = new BinaryExpression(BinaryExpressionTypeEnum.Or,
                                request.Where,
                                new BinaryExpression(BinaryExpressionTypeEnum.Equal,
                                    new ColumnAccess(request.FromAlias, new Identifier(_queryProvider.SqlMapping.GetColumnName(idProperty))),
                                    new Constant(Convert.ChangeType(id, idProperty.PropertyType))));

                        var mapper = new Mapper(_queryProvider);
                        mapper.LoadingPropertyInfos.AddRange(_loadingPropertyInfo.GetSubLoadingPropertyInfoforLazy());
                        mapper.DeclareMap(request, null);

                        foreach (var obj in (System.Collections.IEnumerable)_queryProvider.Execute(mapper, request, typeof(IEnumerable<>).MakeGenericType(elementType)))
                        {
                            _loadedObjects[getId(obj)] = obj;
                        }
                    }

                    if (!_loadedObjects.ContainsKey(_objectId)) throw new SQLException($"Object with id {_objectId} doesn't exist anymore in db.");
                    _loadedObject = _loadedObjects[_objectId];
                }

                invocation.ReturnValue = invocation.Method.Invoke(_loadedObject, invocation.Arguments);
            }
        }
    }
}
