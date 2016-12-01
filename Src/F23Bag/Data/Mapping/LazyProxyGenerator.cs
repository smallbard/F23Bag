using Castle.DynamicProxy;
using F23Bag.Data.DML;
using System;

namespace F23Bag.Data.Mapping
{
    internal class LazyProxyGenerator 
    {
        public static readonly ProxyGenerator ProxyGenerator = new ProxyGenerator();
        private readonly DbQueryProvider _queryProvider;

        public LazyProxyGenerator(DbQueryProvider queryProvider)
        {
            _queryProvider = queryProvider;
        }

        public object GetProxy(LoadingPropertyInfo loadingPropertyInfo, object objectId)
        {
            return ProxyGenerator.CreateClassProxy(loadingPropertyInfo.Property.PropertyType, new SingleObjectInterceptor(_queryProvider, loadingPropertyInfo, objectId));
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
                    if (invocation.Method.Name == "get_Id")
                    {
                        invocation.ReturnValue = Convert.ChangeType(_objectId, invocation.Method.ReturnType);
                        return;
                    }

                    var elementType = _loadingPropertyInfo.Property.PropertyType;
                    var idProperty = elementType.GetProperty("Id");

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
                    mapper.LoadingPropertyInfos.AddRange(_loadingPropertyInfo.SubLoadingPropertyInfo);
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
                    mapper.LoadingPropertyInfos.AddRange(_loadingPropertyInfo.SubLoadingPropertyInfo);
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
    }
}
