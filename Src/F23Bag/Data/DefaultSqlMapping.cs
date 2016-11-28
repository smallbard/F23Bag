using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using F23Bag.Data.DML;
using System.Text;

namespace F23Bag.Data
{
    public class DefaultSqlMapping : ISQLMapping
    {
        private readonly Dictionary<PropertyInfo, PropertyInfo> _equivalentProperties;
        private readonly IEnumerable<IPropertyMapper> _propertyMappers;

        public DefaultSqlMapping(IEnumerable<IPropertyMapper> propertyMappers)
        {
            _equivalentProperties = new Dictionary<PropertyInfo, PropertyInfo>();
            _propertyMappers = propertyMappers ?? new IPropertyMapper[] { };
        }

        public virtual DMLNode GetSqlEquivalent(Request request, AliasDefinition ownerAlias, PropertyInfo property, bool inOr)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));
            property = GetRealProperty(property);

            if (property.PropertyType != typeof(string) && property.PropertyType.IsClass)
            {
                var alias = request.GetAliasFor(property);
                if (alias == null)
                {
                    var elementType = typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;
                    request.Joins.Add(new Join(
                        inOr ? JoinTypeEnum.Left : JoinTypeEnum.Inner,
                        alias = new DML.AliasDefinition(GetSqlEquivalent(elementType)),
                        GetJoinCondition(request, property, ownerAlias, alias, inOr)));
                }
                else
                    request.GetJoinForAlias(alias).Use++;

                return alias;
            }
            else
                return new ColumnAccess(ownerAlias, new Identifier(GetColumnName(property)));
        }

        public virtual string GetColumnName(PropertyInfo property)
        {
            if ((property.PropertyType.IsClass && property.PropertyType != typeof(string)) || property.PropertyType.IsInterface) return "IDFK_" + property.Name.ToUpper();
            if (property.Name.StartsWith("Id") && property.Name.Length > 2) return "IDFK_" + property.Name.Substring(2).ToUpper();

            var name = new StringBuilder();
            foreach (var c in property.Name)
            {
                if (char.IsUpper(c) && name.Length > 0) name.Append('_');
                name.Append(char.ToUpper(c));
            }

            return name.ToString();
        }

        public virtual DMLNode GetSqlEquivalent(Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return new Identifier(string.Join("", type.Name.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + c : char.ToUpper(c).ToString())));
        }

        public virtual void AddEquivalentProperty(PropertyInfo original, PropertyInfo equivalentProperty)
        {
            _equivalentProperties[equivalentProperty] = original;
        }

        public virtual IEnumerable<IPropertyMapper> GetCustomPropertiesMappers()
        {
            return _propertyMappers;
        }

        private PropertyInfo GetRealProperty(PropertyInfo property)
        {
            if (!_equivalentProperties.ContainsKey(property)) return property;
            return GetRealProperty(_equivalentProperties[property]);
        }

        private DMLNode GetJoinCondition(Request request, PropertyInfo property, AliasDefinition aliasOwner, AliasDefinition aliasElement, bool inOr)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                    GetSqlEquivalent(request, aliasOwner, property.DeclaringType.GetProperty("Id"), inOr),
                    new ColumnAccess(aliasElement, new DML.Identifier(GetColumnName(property))));
            else
                return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                    GetSqlEquivalent(request, aliasElement, property.PropertyType.GetProperty("Id"), inOr),
                    new ColumnAccess(aliasOwner, new Identifier(GetColumnName(property))));
        }
    }
}
