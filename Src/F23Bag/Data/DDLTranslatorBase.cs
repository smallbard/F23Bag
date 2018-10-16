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
        public virtual void Translate(DDLStatement ddlStatement, ISQLMapping sqlMapping, IList<string> objects, IList<string> constraintsAndAlter)
        {
            if (ddlStatement == null) throw new ArgumentNullException(nameof(ddlStatement));
            if (sqlMapping == null) throw new ArgumentNullException(nameof(sqlMapping));
            if (objects == null) throw new ArgumentNullException(nameof(objects));
            if (constraintsAndAlter == null) throw new ArgumentNullException(nameof(constraintsAndAlter));

            if (ddlStatement.StatementType == DDLStatementType.CreateTable)
            {
                if (ddlStatement.ElementType == null) throw new ArgumentException("ElementType not set.", nameof(ddlStatement));

                var tableName = ((Identifier)sqlMapping.GetSqlEquivalent(ddlStatement.ElementType)).IdentifierName;

                var sql = new StringBuilder("CREATE TABLE ").Append(tableName).Append('(');
                foreach (var property in ddlStatement.ElementType.GetProperties().Where(p => p.GetCustomAttribute<TransientAttribute>() == null && p.GetCustomAttribute<InversePropertyAttribute>() == null))
                {
                    bool isAlter;
                    var columnDefinition = GetColumnDefinition(sqlMapping, property, out isAlter);
                    if (isAlter)
                        constraintsAndAlter.Add(columnDefinition.ToString());
                    else
                        sql.Append(columnDefinition).Append(',');
                }
                sql.Remove(sql.Length - 1, 1).Append(')');

                objects.Add(sql.ToString());
                return;
            }
            else if (ddlStatement.StatementType == DDLStatementType.AddColumn)
            {
                if (ddlStatement.Property == null) throw new ArgumentException("Property not set.", nameof(ddlStatement));

                bool isAlter;
                var columnDefinition = GetColumnDefinition(sqlMapping, ddlStatement.Property, out isAlter);
                if (isAlter)
                    constraintsAndAlter.Add(columnDefinition.ToString());
                else
                    constraintsAndAlter.Add(new StringBuilder("ALTER TABLE ").Append(((Identifier)sqlMapping.GetSqlEquivalent(ddlStatement.Property.ReflectedType)).IdentifierName).Append(" ADD COLUMN ").Append(columnDefinition).ToString());
                return;
            }

            throw new NotImplementedException();
        }

        protected abstract StringBuilder GetColumnDefinition(ISQLMapping sqlMapping, PropertyInfo property, out bool isAlter);

        protected virtual string GetSqlTypeName(Type type)
        {
            type = type.GetDbValueType();

            if (type == typeof(int) || type == typeof(int?))
                return "INTEGER";
            else if (type == typeof(long) || type == typeof(long?))
                return "BIGINT";
            else if (type == typeof(short) || type == typeof(short?))
                return "SMALLINT";
            else if (type == typeof(bool) || type == typeof(bool?))
                return "SMALLINT";
            else if (type == typeof(string))
                return "NVARCHAR(100)";
            else if (type == typeof(char))
                return "CHAR";
            else if (type == typeof(DateTime) || type == typeof(DateTime?))
                return "TIMESTAMP";
            else if (type.BaseType == typeof(Enum))
                return "INT";
            else if (type == typeof(decimal) || type == typeof(decimal?))
                return " NUMERIC(18,7)";
            else if (type == typeof(byte[]))
                return "BLOB";
            else if (type == typeof(double) || type == typeof(double?))
                return "FLOAT";
            else
                throw new NotImplementedException();
        }
    }
}
