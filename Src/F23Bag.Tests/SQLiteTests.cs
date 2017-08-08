using Microsoft.VisualStudio.TestTools.UnitTesting;
using F23Bag.Data;
using F23Bag.SQLite;
using System.Collections.Generic;

namespace F23Bag.Tests
{
    [TestClass]
    public class SQLiteTests : F23Bag.Tests.DatabaseProviderTests
    {
        private SQLiteProvider _provider;

        protected override ISQLProvider Provider
        {
            get
            {
                return _provider ?? (_provider = new SQLiteProvider());
            }
        }

        protected override bool EnableTests
        {
            get
            {
                return true;
            }
        }

        [TestInitialize]
        public override void CreateTables()
        {
            ExecuteNonQuery("CREATE TABLE OBJECT1_BIS(ID INTEGER NOT NULL PRIMARY KEY AUTOINCREMENT, NAME NVARCHAR(100), IDFK_OBJET2 INTEGER)");

            var objectsScripts = new List<string>();
            var constraintsAndAlter = new List<string>();

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Object2)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Object3)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(Object1)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(ChildObject)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);
            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(ParentObject)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            Provider.GetDDLTranslator().Translate(new Data.DDL.DDLStatement(Data.DDL.DDLStatementType.CreateTable, typeof(ObjectWithDbValueType)), new DefaultSqlMapping(null), objectsScripts, constraintsAndAlter);

            foreach (var sql in objectsScripts) ExecuteNonQuery(sql);
            foreach(var sql in constraintsAndAlter) ExecuteNonQuery(sql);

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
