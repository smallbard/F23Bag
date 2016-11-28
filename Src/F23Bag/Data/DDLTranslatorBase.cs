using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using F23Bag.Data.DDL;
using F23Bag.Data.DML;
using System.Reflection;

namespace F23Bag.Data
{
    public abstract class DDLTranslatorBase : IDDLTranslator
    {
        public virtual IEnumerable<string> Translate(DDLStatement ddlStatement, ISQLMapping sqlMapping)
        {
            if (ddlStatement.StatementType == DDLStatementType.CreateTable)
            {
                if (ddlStatement.ElementType == null) throw new ArgumentException("ElementType not set.", nameof(ddlStatement));

                var tableName = ((Identifier)sqlMapping.GetSqlEquivalent(ddlStatement.ElementType)).IdentifierName;

                var sqls = new List<StringBuilder>();
                var sql = new StringBuilder("CREATE TABLE ").Append(tableName).Append('(');
                sqls.Add(sql);

                foreach (var property in ddlStatement.ElementType.GetProperties())
                {
                    bool isAlter;
                    var columnDefinition = GetColumnDefinition(sqlMapping, property, out isAlter);
                    if (isAlter)
                        sqls.Add(columnDefinition);
                    else
                        sql.Append(columnDefinition).Append(',');
                }

                sql.Remove(sql.Length - 1, 1).Append(')');
                return sqls.Select(sb => sb.ToString());
            }
            else if (ddlStatement.StatementType == DDLStatementType.AddColumn)
            {
                if (ddlStatement.Property == null) throw new ArgumentException("Property not set.", nameof(ddlStatement));

                bool isAlter;
                var columnDefinition = GetColumnDefinition(sqlMapping, ddlStatement.Property, out isAlter);
                if (isAlter)
                    return new[] { columnDefinition.ToString() };
                else
                    return new[] { new StringBuilder("ALTER TABLE ").Append(((Identifier)sqlMapping.GetSqlEquivalent(ddlStatement.Property.DeclaringType)).IdentifierName).Append(" ADD COLUMN ").Append(columnDefinition).ToString() };
            }

            throw new NotImplementedException();
        }

        protected abstract StringBuilder GetColumnDefinition(ISQLMapping sqlMapping, PropertyInfo property, out bool isAlter);

        protected virtual string GetSqlTypeName(Type type)
        {
            if (type == typeof(int) || type == typeof(int?))
            {
                return "INTEGER";
            }
            else if (type == typeof(short) || type == typeof(short?))
            {
                return "SMALLINT";
            }
            else if (type == typeof(bool) || type == typeof(bool?))
            {
                return "SMALLINT";
            }
            else if (type == typeof(string))
            {
                return "NVARCHAR(100)";
            }
            else if (type == typeof(char))
            {
                return "CHAR";
            }
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
            {
                return "TIMESTAMP";
            }
            else if (type.BaseType == typeof(Enum))
            {
                return "INT";
            }
            else if (type == typeof(decimal) || type == typeof(decimal?))
            {
                return " NUMERIC(18,7)";
            }
            else if (type == typeof(byte[]))
            {
                return "BLOB";
            }
            else if (type == typeof(double) || type == typeof(double?))
            {
                return "FLOAT";
            }
            else
                throw new NotImplementedException();
        }
    }
}
