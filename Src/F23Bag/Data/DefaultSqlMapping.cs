using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using F23Bag.Data.DML;
using System.Text;
using System.Text.RegularExpressions;
using System.Globalization;

namespace F23Bag.Data
{
    public class DefaultSqlMapping : ISQLMapping
    {
        private readonly static Dictionary<Type, IEnumerable<PropertyInfo>> _mappedProperties = new Dictionary<Type, IEnumerable<PropertyInfo>>();
        private readonly static Regex _removedGenericArgumentCount = new Regex("`[0-9]", RegexOptions.Compiled);
        private readonly IEnumerable<IPropertyMapper> _propertyMappers;
        private readonly Dictionary<string, DMLNode> _registeredEquivalents = new Dictionary<string, DMLNode>();

        public DefaultSqlMapping(IEnumerable<IPropertyMapper> propertyMappers)
        {
            _propertyMappers = propertyMappers ?? new IPropertyMapper[] { };
        }

        public virtual DMLNode GetSqlEquivalent(Request request, AliasDefinition ownerAlias, PropertyInfo property, bool inOr)
        {
            if (request == null) throw new ArgumentNullException(nameof(request));
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (ownerAlias != null && _registeredEquivalents.ContainsKey(property.DeclaringType.Name + "." + property.Name))
            {
                return _registeredEquivalents[property.DeclaringType.Name + "." + property.Name];
            }

            if (property.PropertyType.IsEntityOrCollection())
            {
                var propertyAccess = new PropertyAccess(ownerAlias, property);

                var alias = request.GetAliasFor(propertyAccess);
                if (alias == null)
                {
                    var elementType = property.PropertyType.IsCollection() ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;

                    //if subrequest, search the request with the ownerAlias
                    if (request.ParentRequest != null)
                    {
                        var r = request;
                        while (r != null)
                        {
                            if (r.FromAlias == ownerAlias || r.Joins.Any(j => j.Alias == ownerAlias)) break;
                            r = r.ParentRequest;
                        }
                        inOr = r != request && r != null;
                        if (r != null) request = r;
                    }

                    request.Joins.Add(new Join(
                        inOr ? JoinTypeEnum.Left : JoinTypeEnum.Inner,
                        alias = new DML.AliasDefinition(GetSqlEquivalent(elementType)),
                        GetJoinCondition(request, property, ownerAlias, alias, inOr)));

                    alias.Equivalents.Add(propertyAccess);
                }
                else
                    request.GetJoinForAlias(alias).Use++;

                return alias;
            }
            else
                return new ColumnAccess(ownerAlias, new Identifier(GetColumnName(property)));
        }

        public virtual void SetSqlEquivalent(PropertyInfo property, DMLNode sqlEquivalent)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            if (sqlEquivalent == null) throw new ArgumentNullException(nameof(sqlEquivalent));

            _registeredEquivalents[property.DeclaringType.Name + "." +  property.Name] = sqlEquivalent;
        }

        public virtual string GetColumnName(PropertyInfo property)
        {
            var inversePropertyAtt = property.GetCustomAttribute<InversePropertyAttribute>();
            if (inversePropertyAtt != null) property = inversePropertyAtt.InverseProperty;

            if (property.PropertyType.IsEntityOrCollection()) return "IDFK_" + property.Name.ToUpper(CultureInfo.InvariantCulture);
            if (property.Name.StartsWith("Id", StringComparison.OrdinalIgnoreCase) && property.Name.Length > 2) return "IDFK_" + property.Name.Substring(2).ToUpper(CultureInfo.InvariantCulture);

            var name = new StringBuilder();
            foreach (var c in property.Name)
            {
                if (char.IsUpper(c) && name.Length > 0) name.Append('_');
                name.Append(char.ToUpper(c, CultureInfo.InvariantCulture));
            }

            return name.ToString();
        }

        public virtual DMLNode GetSqlEquivalent(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return new Identifier(GetTableName(type));
        }

        public virtual IEnumerable<IPropertyMapper> GetCustomPropertiesMappers()
        {
            return _propertyMappers;
        }

        public virtual PropertyInfo GetIdProperty(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type.GetProperty("Id");
        }

        public virtual IEnumerable<PropertyInfo> GetMappedSimpleProperties(Type type)
        {
            lock (_mappedProperties)
                if (_mappedProperties.ContainsKey(type))
                    return _mappedProperties[type];
                else
                    return _mappedProperties[type] = GetMappedPropertiesWithoutCache(type).ToList();
        }

        protected virtual IEnumerable<PropertyInfo> GetMappedPropertiesWithoutCache(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            return type.GetProperties().Where(p => p.PropertyType.IsSimpleMappedType() && p.GetCustomAttribute<TransientAttribute>() == null);
        }

        protected virtual string GetTableName(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));
            var tableName = string.Join("",
                _removedGenericArgumentCount.Replace(string.Join("", new string[] { type.Name }.Union(type.GetGenericArguments().Select(t => t.Name))), "")
                    .Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + c : char.ToUpper(c, CultureInfo.InvariantCulture).ToString()));
            if (tableName.EndsWith("_PROXY", StringComparison.OrdinalIgnoreCase)) tableName = tableName.Substring(0, tableName.Length - "_PROXY".Length);
            return tableName;
        }

        private DMLNode GetJoinCondition(Request request, PropertyInfo property, AliasDefinition aliasOwner, AliasDefinition aliasElement, bool inOr)
        {
            if (property.PropertyType.IsCollection())
                return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                    GetSqlEquivalent(request, aliasOwner, GetIdProperty(property.ReflectedType), inOr),
                    new ColumnAccess(aliasElement, new DML.Identifier(GetColumnName(property))));
            else
            {
                var inversePropertyAtt = property.GetCustomAttribute<InversePropertyAttribute>();
                if (inversePropertyAtt != null && !inversePropertyAtt.InverseProperty.PropertyType.IsCollection())
                {
                    return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                        new ColumnAccess(aliasElement, new Identifier(GetColumnName(inversePropertyAtt.InverseProperty))),
                        new ColumnAccess(aliasOwner, new Identifier(GetColumnName(GetIdProperty(property.ReflectedType)))));
                }

                return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                    GetSqlEquivalent(request, aliasElement, GetIdProperty(property.PropertyType), inOr),
                    new ColumnAccess(aliasOwner, new Identifier(GetColumnName(property))));
            }
        }
    }
}
