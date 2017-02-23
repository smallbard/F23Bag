using F23Bag.Data;
using System;
using System.Linq;
using System.Text;
using F23Bag.Data.DML;
using System.Reflection;

namespace F23Bag.ISeries
{
    internal class ISeriesDDLTranslator : DDLTranslatorBase
    {
        private readonly bool _inUnitTest = AppDomain.CurrentDomain.GetAssemblies().Any(a => a.FullName.StartsWith("Microsoft.VisualStudio.QualityTools.UnitTestFramework"));

        protected override StringBuilder GetColumnDefinition(ISQLMapping sqlMapping, PropertyInfo property, out bool isAlter)
        {
            var sql = new StringBuilder();
            isAlter = false;

            var columnName = sqlMapping.GetColumnName(property);
            if (sqlMapping.GetIdProperty(property.DeclaringType).Name.Equals(property.Name) && property.PropertyType == typeof(int))
                sql.Append(columnName).Append(" INTEGER NOT NULL GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1) PRIMARY KEY");
            else if ((property.PropertyType.IsClass || property.PropertyType.IsInterface) && property.PropertyType != typeof(string))
            {
                if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                {
                    var idProperty = sqlMapping.GetIdProperty(property.DeclaringType);
                    sql.Append("ALTER TABLE ")
                        .Append(((Identifier)sqlMapping.GetSqlEquivalent(property.PropertyType.GetGenericArguments()[0])).IdentifierName)
                        .Append(" ADD COLUMN ")
                        .Append(columnName)
                        .Append(' ')
                        .Append(GetSqlTypeName(idProperty.PropertyType));

                    if (!_inUnitTest)
                        sql.Append(" REFERENCES ").Append(((Identifier)sqlMapping.GetSqlEquivalent(property.DeclaringType)).IdentifierName).Append('(').Append(sqlMapping.GetColumnName(idProperty)).Append(')');

                    isAlter = true;
                }
                else
                {
                    sql.Append(columnName).Append(' ').Append(GetSqlTypeName(sqlMapping.GetIdProperty(property.PropertyType).PropertyType));
                    if (!_inUnitTest)
                        sql.Append(" REFERENCES ").Append(((Identifier)sqlMapping.GetSqlEquivalent(property.PropertyType)).IdentifierName).Append('(').Append(sqlMapping.GetColumnName(sqlMapping.GetIdProperty(property.PropertyType))).Append(')');
                }
            }
            else
            {
                var isNullable = property.PropertyType.IsGenericType && property.PropertyType.GetGenericTypeDefinition() == typeof(Nullable<>);
                var sqlTypeName = GetSqlTypeName(property.PropertyType);
                sql.Append(columnName).Append(' ').Append(sqlTypeName);
                if (!isNullable) sql.Append(" NOT NULL");
            }

            return sql;
        }
    }
}
