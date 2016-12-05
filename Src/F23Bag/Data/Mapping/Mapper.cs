using F23Bag.Data.DML;
using F23Bag.Domain;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data.Mapping
{
    internal class Mapper
    {
        private readonly ISQLMapping _sqlMapping;
        private readonly DbQueryProvider _queryProvider;
        private readonly Dictionary<LoadingPropertyInfo, int> _lazyProperties;
        private int _selectMainSimplePropertiesCount;
        
        public Mapper(DbQueryProvider queryProvider)
        {
            _sqlMapping = queryProvider.SqlMapping;
            _queryProvider = queryProvider;
            _lazyProperties = new Dictionary<LoadingPropertyInfo, int>();
            LoadingPropertyInfos = new List<LoadingPropertyInfo>();
        }

        public List<LoadingPropertyInfo> LoadingPropertyInfos { get; private set; }

        public void DontLoad(PropertyInfo[] notLoadedProperties)
        {
            foreach (var lpi in LoadingPropertyInfos.ToList().Where(l => l.Depth + 1 >= notLoadedProperties.Length))
            {
                var lpiLoc = lpi;
                var remove = true;
                for (var i = 0; i < notLoadedProperties.Length; i++)
                {
                    if (lpiLoc.Property != notLoadedProperties[i])
                    {
                        remove = false;
                        break;
                    }
                    lpiLoc = lpiLoc.SubLoadingPropertyInfo.FirstOrDefault();
                }

                if (remove) LoadingPropertyInfos.Remove(lpi);
            }
        }

        public Request DeclareMap(Request request, Expression expression)
        {
            if (request.RequestType == RequestType.InsertSelect || request.RequestType == RequestType.InsertValues || request.RequestType == RequestType.Update || request.RequestType == RequestType.Delete) return request;

            if (request.Select.Count == 0)
            {
                if (request.GroupBy.Count == 1)
                    request.Select.Add(new SelectInfo(request.GroupBy[0], null));
                else
                {
                    var elementType = request.ProjectionType ?? (expression.Type.GetGenericArguments().Length == 0 ? expression.Type : expression.Type.GetGenericArguments()[0]);
                    if (request.GroupBy.Count > 1) elementType = elementType.GetGenericArguments()[0];
                    AddSelectForSimpleProperties(request, elementType, request.FromAlias);
                }
            }

            _selectMainSimplePropertiesCount = request.Select.Count;

            if (LoadingPropertyInfos.Count > 0)
            {
                LoadingPropertyInfo.RegroupLoadingInfo(LoadingPropertyInfos);
                AddLoading(request, request.FromAlias, LoadingPropertyInfos);
            }

            if ((request.Take > 0 || request.Skip > 0) && request.Orders.Count == 0)
                for (var i = 0; i < request.Select.Count; i++)
                    if (request.Select[i].Property.Name == "Id")
                    {
                        request.Orders.Add(new OrderElement(request.Select[i].SelectSql, true));
                        break;
                    }

            return request;
        }

        public object GetMainId(IDataRecord reader, Request request)
        {
            var idIndex = -1;
            for (var i = 0; i < request.Select.Count; i++)
                if (request.Select[i].Property.Name == "Id")
                {
                    idIndex = i;
                    break;
                }

            if (idIndex < 0) throw new SQLException("Property Id not found.");

            return reader[idIndex];
        }

        public void Map(object o, IDataRecord reader, Request request, bool firstRead)
        {

            if (firstRead)
            {
                var mappers = _sqlMapping.GetCustomPropertiesMappers();
                for (var i = 0; i < _selectMainSimplePropertiesCount; i++)
                {
                    var mapper = mappers.FirstOrDefault(m => m.Accept(request.Select[i].Property));
                    if (mapper != null)
                        mapper.Map(o, request.Select[i].Property, reader, i);
                    else if (request.Select[i].Property.PropertyType.IsEnum)
                        request.Select[i].Property.SetValue(o, Convert.ToInt32(reader[i]));
                    else
                        request.Select[i].Property.SetValue(o, Convert.ChangeType(DBNull.Value.Equals(reader[i]) ? null : reader[i], request.Select[i].Property.PropertyType));
                }
            }

            var selectIndex = _selectMainSimplePropertiesCount;
            MapLoading(o, reader, request, firstRead, ref selectIndex, LoadingPropertyInfos);
        }

        private void MapLoading(object o, IDataRecord reader, Request request, bool firstRead, ref int selectIndex, List<LoadingPropertyInfo> loadingPropertyInfos)
        {
            foreach (var lpi in loadingPropertyInfos)
            {
                var isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(lpi.Property.PropertyType);
                if (lpi.IsLazyLoading)
                {
                    if (!firstRead) continue;

                    if (isCollection)
                        lpi.Property.SetValue(o, new LazyProxyGenerator(_queryProvider).GetProxyForCollection(lpi, o.GetType().GetProperty("Id").GetValue(o)));
                    else
                    {
                        var objectId = reader[_lazyProperties[lpi]];
                        if (!DBNull.Value.Equals(objectId)) lpi.Property.SetValue(o, new LazyProxyGenerator(_queryProvider).GetProxy(lpi, objectId));
                    }
                }
                else
                {
                    var elementType = isCollection ? lpi.Property.PropertyType.GetGenericArguments()[0] : lpi.Property.PropertyType;
                    object element = null;
                    if (firstRead)
                    {
                        var elementIdProperty = elementType.GetProperty("Id");
                        var idSelectIndex = -1;
                        if (!isCollection)
                            for (var i = selectIndex; i < request.Select.Count; i++)
                                if (request.Select[i].Property == elementIdProperty)
                                {
                                    idSelectIndex = i;
                                    break;
                                }

                        if (isCollection || !DBNull.Value.Equals(reader[idSelectIndex]))
                        {
                            element = isCollection ? new CollectionActivator().CreateInstance(lpi.Property.PropertyType) : _queryProvider.Resolve(lpi.Property.PropertyType);
                            lpi.Property.SetValue(o, element);
                            if (isCollection)
                            {
                                var collection = (System.Collections.IList)element;
                                element = Activator.CreateInstance(elementType);
                                collection.Add(element);
                            }
                        }
                    }
                    else if (isCollection)
                    {
                        var idIndex = -1;
                        for (var i = selectIndex; i < request.Select.Count; i++)
                            if (request.Select[i].Property.Name == "Id")
                            {
                                idIndex = i;
                                break;
                            }

                        if (idIndex < 0) throw new SQLException("Property Id not found.");

                        var collection = (System.Collections.IList)lpi.Property.GetValue(o);
                        element = collection[0];
                        var idProperty = request.Select[idIndex].Property;
                        if (firstRead = !idProperty.GetValue(element).Equals(reader[idIndex]))
                        {
                            element = Activator.CreateInstance(elementType);
                            collection.Add(element);
                        }
                    }

                    if (firstRead && element != null)
                    {
                        var mappers = _sqlMapping.GetCustomPropertiesMappers();
                        foreach (var property in elementType.GetProperties().Where(p => IsSimpleMappedProperty(p)))
                        {
                            var indexForLambda = selectIndex;
                            var mapper = mappers.FirstOrDefault(m => m.Accept(request.Select[indexForLambda].Property));
                            if (mapper != null)
                                mapper.Map(o, request.Select[selectIndex].Property, reader, selectIndex);
                            else
                                request.Select[selectIndex].Property.SetValue(element, Convert.ChangeType(DBNull.Value.Equals(reader[selectIndex]) ? null : reader[selectIndex], request.Select[selectIndex].Property.PropertyType));
                            selectIndex++;
                        }
                    }

                    if (element == null) while (selectIndex < request.Select.Count && request.Select[selectIndex].Property.DeclaringType == elementType) selectIndex++;

                    MapLoading(element, reader, request, firstRead, ref selectIndex, lpi.SubLoadingPropertyInfo);
                }
            }
        }

        private void AddSelectForSimpleProperties(Request request, Type elementType, AliasDefinition alias)
        {
            var mappers = _sqlMapping.GetCustomPropertiesMappers();
            foreach (var property in elementType.GetProperties().Where(p => IsSimpleMappedProperty(p)))
            {
                var mapper = mappers.FirstOrDefault(m => m.Accept(property));
                if (mapper != null)
                    request.Select.Add(mapper.DeclareMap(request, property, alias));
                else
                    request.Select.Add(new SelectInfo(_sqlMapping.GetSqlEquivalent(request, alias, property, false), property));
            }
        }

        private static bool IsSimpleMappedProperty(PropertyInfo p)
        {
            return (!p.PropertyType.IsClass && !p.PropertyType.IsInterface) || p.PropertyType == typeof(string) && p.GetCustomAttribute<TransientAttribute>() == null;
        }

        private void AddLoading(Request request, AliasDefinition alias, List<LoadingPropertyInfo> loadingPropertyInfos)
        {
            foreach (var lpi in loadingPropertyInfos.OrderBy(l => l.Depth))
                if (lpi.IsLazyLoading)
                {
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(lpi.Property.PropertyType)) continue;
                    _lazyProperties[lpi] = request.Select.Count;
                    request.Select.Add(new SelectInfo(new ColumnAccess(alias, new Identifier(_sqlMapping.GetColumnName(lpi.Property))), lpi.Property));
                }
                else
                {
                    var elementType = lpi.Property.PropertyType;
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(lpi.Property.PropertyType)) elementType = elementType.GetGenericArguments()[0];
                    var newAlias = (AliasDefinition)_sqlMapping.GetSqlEquivalent(request, alias, lpi.Property, true);

                    AddSelectForSimpleProperties(request, elementType, newAlias);

                    if (lpi.SubLoadingPropertyInfo.Count > 0) AddLoading(request, newAlias, lpi.SubLoadingPropertyInfo);
                }
        }
    }
}
