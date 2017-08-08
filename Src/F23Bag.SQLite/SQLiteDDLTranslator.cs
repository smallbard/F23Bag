using F23Bag.Data;
using F23Bag.Data.DML;
using System;
using System.Reflection;
using System.Text;

namespace F23Bag.SQLite
{
    internal class SQLiteDDLTranslator : DDLTranslatorBase
    {
        protected override StringBuilder GetColumnDefinition(ISQLMapping sqlMapping, PropertyInfo property, out bool isAlter)
        {
            var sql = new StringBuilder();
            isAlter = false;

            var columnName = sqlMapping.GetColumnName(property);
            if (sqlMapping.GetIdProperty(property.DeclaringType).Name.Equals(property.Name) && property.PropertyType == typeof(int))
                sql.Append(columnName).Append(" INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT");
            else if (property.PropertyType.IsEntityOrCollection())
            {
                if (property.PropertyType.IsCollection())
                {
                    var idProperty = sqlMapping.GetIdProperty(property.DeclaringType);
                    sql.Append("ALTER TABLE ")
                        .Append(((Identifier)sqlMapping.GetSqlEquivalent(property.PropertyType.GetGenericArguments()[0])).IdentifierName)
                        .Append(" ADD COLUMN ")
                        .Append(columnName)
                        .Append(' ')
                        .Append(GetSqlTypeName(idProperty.PropertyType))
                        .Append(" REFERENCES ").Append(((Identifier)sqlMapping.GetSqlEquivalent(property.DeclaringType)).IdentifierName).Append('(').Append(sqlMapping.GetColumnName(idProperty)).Append(')');

                    isAlter = true;
                }
                else
                    sql.Append(columnName).Append(' ').Append(GetSqlTypeName(sqlMapping.GetIdProperty(property.PropertyType).PropertyType))
                        .Append(" REFERENCES ").Append(((Identifier)sqlMapping.GetSqlEquivalent(property.PropertyType)).IdentifierName).Append('(').Append(sqlMapping.GetColumnName(sqlMapping.GetIdProperty(property.PropertyType))).Append(')');
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

        protected override string GetSqlTypeName(Type type)
        {
            type = type.GetDbValueType();

            if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "INTEGER";
            else
                return base.GetSqlTypeName(type);
        }
    }
}
