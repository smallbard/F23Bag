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
        private readonly IEnumerable<IPropertyMapper> _propertyMappers;

        public DefaultSqlMapping(IEnumerable<IPropertyMapper> propertyMappers)
        {
            _propertyMappers = propertyMappers ?? new IPropertyMapper[] { };
        }

        public virtual DMLNode GetSqlEquivalent(Request request, AliasDefinition ownerAlias, PropertyInfo property, bool inOr)
        {
            if (property == null) throw new ArgumentNullException(nameof(property));

            if (property.PropertyType != typeof(string) && property.PropertyType.IsClass)
            {
                var alias = request.GetAliasFor(property);
                if (alias == null)
                {
                    var elementType = typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType) ? property.PropertyType.GetGenericArguments()[0] : property.PropertyType;

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

                    alias.Equivalents.Add(property);
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

            return new Identifier(GetTableName(type));
        }

        public virtual IEnumerable<IPropertyMapper> GetCustomPropertiesMappers()
        {
            return _propertyMappers;
        }

        public virtual PropertyInfo GetIdProperty(Type type)
        {
            return type.GetProperty("Id");
        }

        protected virtual string GetTableName(Type type)
        {
            return string.Join("", type.Name.Select((c, i) => char.IsUpper(c) && i > 0 ? "_" + c : char.ToUpper(c).ToString()));
        }

        private DMLNode GetJoinCondition(Request request, PropertyInfo property, AliasDefinition aliasOwner, AliasDefinition aliasElement, bool inOr)
        {
            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                    GetSqlEquivalent(request, aliasOwner, GetIdProperty(property.DeclaringType), inOr),
                    new ColumnAccess(aliasElement, new DML.Identifier(GetColumnName(property))));
            else
                return new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal,
                    GetSqlEquivalent(request, aliasElement, GetIdProperty(property.PropertyType), inOr),
                    new ColumnAccess(aliasOwner, new Identifier(GetColumnName(property))));
        }
    }
}
