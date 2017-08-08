using System;
using F23Bag.Data;
using F23Bag.Tests;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;

namespace F23Bag.ISeries.Tests
{
    [TestClass]
    public class ISeriesTests : DatabaseProviderTests
    {
        private ISQLProvider _provider;

        protected override ISQLProvider Provider
        {
            get
            {
                return _provider ?? (_provider = new ISeriesProvider(System.Configuration.ConfigurationManager.ConnectionStrings["ISeries"].ConnectionString));
            }
        }

        protected override bool EnableTests
        {
            get
            {
                return System.Configuration.ConfigurationManager.ConnectionStrings["ISeries"] != null && !string.IsNullOrEmpty(System.Configuration.ConfigurationManager.ConnectionStrings["ISeries"].ConnectionString);
            }
        }

        [TestInitialize]
        public override void CreateTables()
        {
            if (!EnableTests) return;

            ExecuteNonQuery("CREATE TABLE OBJECT1_BIS(ID INTEGER NOT NULL GENERATED ALWAYS AS IDENTITY (START WITH 1 INCREMENT BY 1) PRIMARY KEY, NAME NVARCHAR(100), IDFK_OBJET2 INTEGER)");

            var objectsScripts = new List<string>();
            var constraintsAndAlter = new List<string>();

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Object2)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Object3)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Object1)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(ChildObject)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(ParentObject)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(ObjectWithDbValueType)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            foreach (var sql in objectsScripts) ExecuteNonQuery(sql);
            foreach (var sql in constraintsAndAlter) ExecuteNonQuery(sql);

            ExecuteNonQuery("INSERT INTO OBJECT2(VALUE2) VALUES(1.2)");
            ExecuteNonQuery("INSERT INTO OBJECT2(VALUE2) VALUES(2.2)");
            ExecuteNonQuery("INSERT INTO OBJECT2(VALUE2) VALUES(3.2)");

            ExecuteNonQuery("INSERT INTO OBJECT1(NAME, NULL_NUMBER, IDFK_OBJET2) VALUES('obj1', NULL, 1)");
            ExecuteNonQuery("INSERT INTO OBJECT1(NAME, NULL_NUMBER, IDFK_OBJET2) VALUES('obj2', 5, 2)");
            ExecuteNonQuery("INSERT INTO OBJECT1(NAME, NULL_NUMBER, IDFK_OBJET2) VALUES('obj3', 7, 3)");

            ExecuteNonQuery("INSERT INTO OBJECT1_BIS(NAME, IDFK_OBJET2) VALUES('obj1', 1)");
            ExecuteNonQuery("INSERT INTO OBJECT1_BIS(NAME, IDFK_OBJET2) VALUES('obj2', 2)");
            ExecuteNonQuery("INSERT INTO OBJECT1_BIS(NAME, IDFK_OBJET2) VALUES('obj3', 3)");

            ExecuteNonQuery("INSERT INTO OBJECT3(VALUE3, IDFK_OBJET2, IDFK_OBJET3LIST) VALUES(1.3, 3, 1)");
            ExecuteNonQuery("INSERT INTO OBJECT3(VALUE3, IDFK_OBJET2, IDFK_OBJET3LIST) VALUES(2.3, 2, 1)");
            ExecuteNonQuery("INSERT INTO OBJECT3(VALUE3, IDFK_OBJET2, IDFK_OBJET3LIST) VALUES(3.3, 1, 2)");
        }

        [TestCleanup]
        public override void DropTables()
        {
            if (!EnableTests) return;

            ExecuteNonQuery("DROP TABLE OBJECT1");
            ExecuteNonQuery("DROP TABLE OBJECT1_BIS");
            ExecuteNonQuery("DROP TABLE OBJECT2");
            ExecuteNonQuery("DROP TABLE OBJECT3");

            ExecuteNonQuery("DROP TABLE PARENT_OBJECT");
            ExecuteNonQuery("DROP TABLE CHILD_OBJECT");

            ExecuteNonQuery("DROP TABLE OBJECT_WITH_DB_VALUE_TYPE");
        }
    }
}
