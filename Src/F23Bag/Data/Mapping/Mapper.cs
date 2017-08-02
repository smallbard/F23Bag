using F23Bag.Data.DML;
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
        private readonly Dictionary<LoadingPropertyInfo, Tuple<int,int>> _loadingPropertyInfoSelectIndexes;
        private readonly LazyProxyGenerator _lazyProxyGenerator;
        private IEnumerable<IPropertyMapper> _mappers;

        public Mapper(DbQueryProvider queryProvider)
        {
            _sqlMapping = queryProvider.SqlMapping;
            _mappers = _sqlMapping.GetCustomPropertiesMappers();
            _queryProvider = queryProvider;
            _lazyProperties = new Dictionary<LoadingPropertyInfo, int>();
            _loadingPropertyInfoSelectIndexes = new Dictionary<LoadingPropertyInfo, Tuple<int, int>>();
            _lazyProxyGenerator = new LazyProxyGenerator(_queryProvider);
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
                    request.Select.Add(new SelectInfo(request.GroupBy[0], null, true));
                else
                {
                    var elementType = request.ProjectionType ?? (expression.Type.GetGenericArguments().Length == 0 ? expression.Type : expression.Type.GetGenericArguments()[0]);
                    if (request.GroupBy.Count > 1) elementType = elementType.GetGenericArguments()[0];
                    AddSelectForSimpleProperties(request, elementType, request.FromAlias);
                }
            }
            
            if (LoadingPropertyInfos.Count > 0)
            {
                if (request.ProjectionType != null && request.ProjectionType != LoadingPropertyInfos[0].Property.ReflectedType)
                {
                    // should have been a .Select(o => new { ... }
                    LoadingPropertyInfos.Clear();
                }
                else
                {
                    LoadingPropertyInfo.RegroupLoadingInfo(LoadingPropertyInfos);
                    AddLoading(request, request.FromAlias, LoadingPropertyInfos);
                }
            }

            if ((request.Take > 0 || request.Skip > 0) && request.Orders.Count == 0)
                for (var i = 0; i < request.Select.Count; i++)
                {
                    var idProperty = _sqlMapping.GetIdProperty(request.Select[i].Property.ReflectedType);
                    if (idProperty != null && idProperty.Name.Equals(request.Select[i].Property.Name))
                    {
                        request.Orders.Add(new OrderElement(request.Select[i].SelectSql, true));
                        break;
                    }
                }

            return request;
        }

        public object GetMainId(IDataRecord reader, Request request)
        {
            var idIndex = -1;
            for (var i = 0; i < request.Select.Count; i++)
            {
                var idProperty = _sqlMapping.GetIdProperty(request.Select[i].Property.ReflectedType);
                if (idProperty != null && idProperty.Name.Equals(request.Select[i].Property.Name))
                {
                    idIndex = i;
                    break;
                }
            }

            if (idIndex < 0) return null;

            return reader[idIndex];
        }

        public void Map(object o, IDataRecord reader, Request request, bool firstRead)
        {
            if (firstRead)
                MapProperties(reader, request, o.GetType(), o, 0);

            MapLoading(o, reader, request, firstRead, LoadingPropertyInfos);
        }

        private void MapLoading(object o, IDataRecord reader, Request request, bool firstRead, List<LoadingPropertyInfo> loadingPropertyInfos)
        {
            foreach (var lpi in loadingPropertyInfos)
            {
                var isCollection = typeof(System.Collections.IEnumerable).IsAssignableFrom(lpi.Property.PropertyType);
                if (lpi.LazyLoadingType != LazyLoadingType.None)
                {
                    if (!firstRead) continue;

                    if (isCollection)
                        lpi.SetPropertyValue(o, _lazyProxyGenerator.GetProxyForCollection(lpi, _sqlMapping.GetIdProperty(o.GetType()).GetValue(o)));
                    else if (!DBNull.Value.Equals(reader[_lazyProperties[lpi]]))
                    {
                        var objectId = Convert.ChangeType(reader[_lazyProperties[lpi]], _sqlMapping.GetIdProperty(lpi.Property.PropertyType).PropertyType);
                        lpi.SetPropertyValue(o, _lazyProxyGenerator.GetProxy(lpi, objectId));
                    }
                }
                else
                {
                    var lpiIndexes = _loadingPropertyInfoSelectIndexes[lpi];

                    var elementType = isCollection ? lpi.Property.PropertyType.GetGenericArguments()[0] : lpi.Property.PropertyType;
                    var elementIdProperty = _sqlMapping.GetIdProperty(elementType);
                    var idSelectIndex = -1;
                    for (var i = lpiIndexes.Item1; i < lpiIndexes.Item2 + 1; i++)
                        if (request.Select[i].Property.Name == elementIdProperty.Name)
                        {
                            idSelectIndex = i;
                            break;
                        }

                    if (idSelectIndex < 0) throw new SQLException("Property Id not found.");
                    if (DBNull.Value.Equals(reader[idSelectIndex])) continue;

                    var element = firstRead ? (isCollection ? new CollectionActivator().CreateInstance(lpi.Property.PropertyType) : _queryProvider.Resolve(lpi.Property.PropertyType)) : lpi.Property.GetValue(o);
                    if (firstRead) lpi.SetPropertyValue(o, element);

                    if (isCollection)
                    {
                        var collection = (System.Collections.IList)element;
                        if (collection.Count > 0)
                        {
                            element = collection[collection.Count - 1];
                            var idProperty = request.Select[idSelectIndex].Property;
                            if (firstRead = !idProperty.GetValue(element).Equals(Convert.ChangeType(reader[idSelectIndex], Nullable.GetUnderlyingType(idProperty.PropertyType) ?? idProperty.PropertyType)))
                            {
                                element = Activator.CreateInstance(elementType);
                                collection.Add(element);
                            }
                        }
                        else
                        {
                            element = Activator.CreateInstance(elementType);
                            collection.Add(element);
                        }
                    }

                    if (firstRead)
                        MapProperties(reader, request, elementType, element, lpiIndexes.Item1);

                    MapLoading(element, reader, request, firstRead, lpi.SubLoadingPropertyInfo);
                }
            }
        }

        private void MapProperties(IDataRecord reader, Request request, Type elementType, object element, int selectIndex)
        {
            do
            {
                var selectInfo = request.Select[selectIndex];
                var mapper = _mappers.FirstOrDefault(m => m.Accept(selectInfo.Property));

                if (mapper != null)
                    mapper.Map(element, selectInfo.Property, reader, selectIndex);
                else if (selectInfo.Property.PropertyType.IsEnum)
                    selectInfo.SetPropertyValue(element, Convert.ToInt32(reader[selectIndex]));
                else
                {
                    try
                    {
                        selectInfo.SetPropertyValue(element, DBNull.Value.Equals(reader[selectIndex]) ? null : Convert.ChangeType(reader[selectIndex], Nullable.GetUnderlyingType(selectInfo.Property.PropertyType) ?? selectInfo.Property.PropertyType));
                    }
                    catch (NullReferenceException)
                    {
                        if (DBNull.Value.Equals(reader[selectIndex]))
                        {
                            throw new SQLException($"{selectInfo.Property.Name} does not accept null (NULL was found in database).");
                        }
                        else
                            throw;
                    }
                }
                selectIndex++;
            }
            while (selectIndex < request.Select.Count && request.Select[selectIndex].Property != null && !request.Select[selectIndex].IsNewElement);
        }

        private void AddSelectForSimpleProperties(Request request, Type elementType, AliasDefinition alias)
        {
            var mappers = _sqlMapping.GetCustomPropertiesMappers();
            var isNewElement = true;
            foreach (var property in _sqlMapping.GetMappedSimpleProperties(elementType))
            {
                var mapper = mappers.FirstOrDefault(m => m.Accept(property));
                if (mapper != null)
                    request.Select.Add(mapper.DeclareMap(request, property, alias, isNewElement));
                else
                    request.Select.Add(new SelectInfo(_sqlMapping.GetSqlEquivalent(request, alias, property, false), property, isNewElement));

                isNewElement = false; ;
            }
        }

        private void AddLoading(Request request, AliasDefinition alias, List<LoadingPropertyInfo> loadingPropertyInfos)
        {
            foreach (var lpi in loadingPropertyInfos.OrderBy(l => l.Depth))
                if (lpi.LazyLoadingType != LazyLoadingType.None)
                {
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(lpi.Property.PropertyType)) continue;
                    _lazyProperties[lpi] = request.Select.Count;
                    request.Select.Add(new SelectInfo(new ColumnAccess(alias, new Identifier(_sqlMapping.GetColumnName(lpi.Property))), lpi.Property, true));
                }
                else
                {
                    var elementType = lpi.Property.PropertyType;
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(lpi.Property.PropertyType)) elementType = elementType.GetGenericArguments()[0];
                    var newAlias = (AliasDefinition)_sqlMapping.GetSqlEquivalent(request, alias, lpi.Property, true);

                    var startSelectIndex = request.Select.Count;
                    if (request.Select.Count != 1 || !(request.Select[0].SelectSql is DML.UnaryExpression)) AddSelectForSimpleProperties(request, elementType, newAlias);
                    _loadingPropertyInfoSelectIndexes[lpi] = Tuple.Create(startSelectIndex, request.Select.Count - 1);

                    if (lpi.SubLoadingPropertyInfo.Count > 0) AddLoading(request, newAlias, lpi.SubLoadingPropertyInfo);
                }
        }
    }
}
