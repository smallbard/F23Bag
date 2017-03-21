using System;
using System.Reflection;
using F23Bag.Data.DML;
using System.Collections.Generic;

namespace F23Bag.Data
{
    public interface ISQLMapping
    {
        DMLNode GetSqlEquivalent(Type type);

        DMLNode GetSqlEquivalent(Request request, AliasDefinition ownerAlias, PropertyInfo property, bool inOr);

        string GetColumnName(PropertyInfo property);

        IEnumerable<IPropertyMapper> GetCustomPropertiesMappers();

        PropertyInfo GetIdProperty(Type type);

        IEnumerable<PropertyInfo> GetMappedProperties(Type type);
    }
}