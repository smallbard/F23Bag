using F23Bag.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using F23Bag.Data;

namespace F23Bag.SqlServer.Tests
{
    [TestClass]
    public class SqlServerTests : DatabaseProviderTests
    {
        protected override bool EnableTests
        {
            get
            {
                return System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer"] != null && !string.IsNullOrEmpty(System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString);
            }
        }

        protected override ISQLProvider Provider
        {
            get
            {
                return new SqlServerProvider(System.Configuration.ConfigurationManager.ConnectionStrings["SqlServer"].ConnectionString);
            }
        }

        [TestInitialize]
        public override void CreateTables()
        {
            if (!EnableTests) return;

            ExecuteNonQuery("CREATE TABLE OBJET1_BIS(ID INTEGER NOT NULL IDENTITY PRIMARY KEY, NAME NVARCHAR(100), IDFK_OBJET2 INTEGER)");

            var objectsScripts = new List<string>();
            var constraintsAndAlter = new List<string>();

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Objet2)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Objet3)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Objet1)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            foreach (var sql in objectsScripts) ExecuteNonQuery(sql);
            foreach (var sql in constraintsAndAlter) ExecuteNonQuery(sql);

            ExecuteNonQuery("INSERT INTO OBJET2(VALUE2) VALUES(1.2)");
            ExecuteNonQuery("INSERT INTO OBJET2(VALUE2) VALUES(2.2)");
            ExecuteNonQuery("INSERT INTO OBJET2(VALUE2) VALUES(3.2)");

            ExecuteNonQuery("INSERT INTO OBJET1(NAME, NULL_NUMBER, IDFK_OBJET2) VALUES('obj1', NULL, 1)");
            ExecuteNonQuery("INSERT INTO OBJET1(NAME, NULL_NUMBER, IDFK_OBJET2) VALUES('obj2', 5, 2)");
            ExecuteNonQuery("INSERT INTO OBJET1(NAME, NULL_NUMBER, IDFK_OBJET2) VALUES('obj3', 7, 3)");

            ExecuteNonQuery("INSERT INTO OBJET1_BIS(NAME, IDFK_OBJET2) VALUES('obj1', 1)");
            ExecuteNonQuery("INSERT INTO OBJET1_BIS(NAME, IDFK_OBJET2) VALUES('obj2', 2)");
            ExecuteNonQuery("INSERT INTO OBJET1_BIS(NAME, IDFK_OBJET2) VALUES('obj3', 3)");

            ExecuteNonQuery("INSERT INTO OBJET3(VALUE3, IDFK_OBJET2, IDFK_OBJET3LIST) VALUES(1.3, 3, 1)");
            ExecuteNonQuery("INSERT INTO OBJET3(VALUE3, IDFK_OBJET2, IDFK_OBJET3LIST) VALUES(2.3, 2, 1)");
            ExecuteNonQuery("INSERT INTO OBJET3(VALUE3, IDFK_OBJET2, IDFK_OBJET3LIST) VALUES(3.3, 1, 2)");
        }

        [TestCleanup]
        public override void DropTables()
        {
            if (!EnableTests) return;

            ExecuteNonQuery("DROP TABLE OBJET3");
            ExecuteNonQuery("DROP TABLE OBJET1");
            ExecuteNonQuery("DROP TABLE OBJET1_BIS");
            ExecuteNonQuery("DROP TABLE OBJET2");
        }
    }
}
