using System;
using System.Collections.Generic;
using System.Linq;

namespace F23Bag.Data
{
    public class DatabaseCreation
    {
        public void CreateDatabase(IEnumerable<Type> entityTypes, ISQLProvider provider, ISQLMapping sqlMapping)
        {
            var objectsScripts = new List<string>();
            var constraintsAndAlter = new List<string>();

            foreach (var entityType in entityTypes)
                provider.GetDDLTranslator().Translate(new F23Bag.Data.DDL.DDLStatement(F23Bag.Data.DDL.DDLStatementType.CreateTable, entityType), sqlMapping, objectsScripts, constraintsAndAlter);

            foreach (var sql in objectsScripts.Concat(constraintsAndAlter))
                using (var cmd = provider.GetConnection().CreateCommand())
                {
                    if (cmd.Connection.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
                    cmd.CommandText = sql;
                    cmd.ExecuteNonQuery();
                }
        }
    }
}
