using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using F23Bag.Data;
using System;

namespace F23Bag.Tests
{
    public abstract class DatabaseProviderTests
    {
        protected abstract ISQLProvider Provider { get; }

        protected abstract bool EnableTests { get; }

        public IQueryable<T> GetQuery<T>()
        {
            return new Query<T>(Provider, new DefaultSqlMapping(null), null, t => Activator.CreateInstance(t));
        }

        [TestMethod]
        public void SimpleQueries()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            Assert.AreEqual(3, GetQuery<Object1>().Where(o => o.Name != null).Count());

            var objs = GetQuery<Object1>().Where(o => ((o.Id > 1 && o.Id < 10) || (o.Id >= 2 && o.Id <= 9)) && o.Id != 0).OrderBy(o => o.Id).ThenBy(o => o.Name).ToList();
            Assert.AreEqual(2, objs.Count);
            Assert.AreEqual(2, objs[0].Id);
            Assert.AreEqual("obj2", objs[0].Name);
            Assert.AreEqual("obj3", objs[1].Name);

            Assert.AreEqual(1, GetQuery<Object1>().Min(o => o.Id));
            Assert.AreEqual(3, GetQuery<Object1>().Max(o => o.Id));
            Assert.AreEqual(2, GetQuery<Object1>().Average(o => o.Id));

            Assert.AreEqual(1, GetQuery<Object1>().Where(o => o.Name.Contains("j2")).Count());
            Assert.AreEqual(3, GetQuery<Object1>().Where(o => o.Name.StartsWith("ob")).Count());
            Assert.AreEqual(1, GetQuery<Object1>().Where(o => o.Name.EndsWith("j3")).Count());
        }

        [TestMethod]
        public void UnitOfWork_UdapteInsert()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));

            uw.Update(GetQuery<Object1>().Where(o => o.Id == 2), o => new Object1() { Name = "testUW" });
            uw.Insert(GetQuery<Object1>().Where(o => o.Id != 2), o => new Object1() { Name = o.Name + "bis" });
            uw.Commit();

            Assert.AreEqual("testUW", GetQuery<Object1>().First(o => o.Id == 2).Name);
            Assert.AreEqual(5, GetQuery<Object1>().Count());
        }

        [TestMethod]
        public void UnitOfWork_DeleteExecute()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Delete(GetQuery<Object3>());
            uw.Delete(GetQuery<Object1>().Where(o => o.Id == 2));
            uw.Delete(GetQuery<Object1>().First(o => o.Id == 1));
            uw.Execute("UPDATE OBJECT1 SET NAME = 'testUWCommand' WHERE ID > 1", null);
            uw.Commit();

            Assert.AreEqual(1, GetQuery<Object1>().Count());
            Assert.AreEqual(1, GetQuery<Object1>().Where(o => o.Name == "testUWCommand").Count());
        }

        [TestMethod]
        public void UnitOfWork_Save()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var newObj = new Object1() { Name = "createdByUw", Objet2 = new Object2() { Value2 = 77 } };
            newObj.Objet3List = new List<Object3>() { new Object3() { Value3 = 88 }, new Object3() { Value3 = 99 } };

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Save(newObj);
            uw.Commit();

            Assert.AreNotEqual(0, newObj.Id);

            var readObj = GetQuery<Object1>().EagerLoad(o => o.Objet2).EagerLoad(o => o.Objet3List).First(o => o.Id == newObj.Id);
            Assert.AreEqual(newObj.Id, readObj.Id);
            Assert.AreNotSame(readObj, newObj);

            Assert.AreEqual("createdByUw", readObj.Name);
            Assert.IsNotNull(readObj.Objet2);
            Assert.AreEqual(77, readObj.Objet2.Value2);
            Assert.IsNotNull(readObj.Objet3List);
            Assert.AreEqual(2, readObj.Objet3List.Count);
            Assert.AreEqual(88, readObj.Objet3List[0].Value3);
            Assert.AreEqual(99, readObj.Objet3List[1].Value3);
        }

        [TestMethod]
        public void UnitOfWork_Differential_Update()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var newObj = new Object1() { Name = "createdByUw", Objet2 = new Object2() { Value2 = 77 } };
            newObj.Objet3List = new List<Object3>() { new Object3() { Value3 = 88 }, new Object3() { Value3 = 99 } };

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Save(newObj);
            uw.Commit();

            var readObj = GetQuery<Object1>().EagerLoad(o => o.Objet2).EagerLoad(o => o.Objet3List).First(o => o.Id == newObj.Id);

            uw.TrackChange(readObj);

            readObj.Name = "updated name";
            readObj.Objet3List[1].Value3 = 1;
            readObj.Objet3List.RemoveAt(0);

            uw.Save(readObj);
            uw.Commit();

            readObj = GetQuery<Object1>().EagerLoad(o => o.Objet2).EagerLoad(o => o.Objet3List).First(o => o.Id == newObj.Id);
            Assert.AreEqual("updated name", readObj.Name);
            Assert.IsNotNull(readObj.Objet2);
            Assert.AreEqual(77, readObj.Objet2.Value2);
            Assert.IsNotNull(readObj.Objet3List);
            Assert.AreEqual(1, readObj.Objet3List.Count);
            Assert.AreEqual(1, readObj.Objet3List[0].Value3);
        }

        [TestMethod]
        public void QueriesWithJoin()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var objs = GetQuery<Object1>().Where(o => o.Objet3List.Any(o3 => o3.Value3 == 1.3)).ToList();
            Assert.AreEqual(1, objs.Count);

            var sum = GetQuery<Object1>().Where(o => o.Id == 1).SelectMany(o => o.Objet3List).Sum(o3 => o3.Value3);

            objs = GetQuery<Object1>().Where(o => o.Objet3List.Sum(o3 => o3.Value3) == sum).ToList();
            Assert.AreEqual(1, objs.Count);
        }

        [TestMethod]
        public void QueriesWithExists()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            Assert.AreEqual(3, GetQuery<Object1>().Where(o => GetQuery<Object1>().Where(o2 => o2.Id == o.Id).Any()).Count());
            Assert.AreEqual(0, GetQuery<Object1>().Where(o => !GetQuery<Object1>().Where(o2 => o2.Id == o.Id).Any()).Count());

            Assert.AreEqual(3, GetQuery<Object1>().Where(o => GetQuery<Object1>().Any(o2 => o2.Id == o.Id)).Count());
            Assert.AreEqual(0, GetQuery<Object1>().Where(o => !GetQuery<Object1>().Any(o2 => o2.Id == o.Id)).Count());
        }

        [TestMethod]
        public void EagerLoading()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var obj1 = GetQuery<Object1>()
                .EagerLoad(o => o.Objet3List.First().Objet2)
                .EagerLoad(o => o.Objet3List)
                .First(o => o.Id == 1);

            Assert.IsNotNull(obj1);
            Assert.AreEqual("obj1", obj1.Name);
            Assert.IsNull(obj1.Objet2);
            Assert.IsNotNull(obj1.Objet3List);
            Assert.AreEqual(2, obj1.Objet3List.Count);

            Assert.AreEqual(1.3, obj1.Objet3List[0].Value3);
            Assert.IsNotNull(obj1.Objet3List[0].Objet2);
            Assert.AreEqual(3.2, obj1.Objet3List[0].Objet2.Value2);

            Assert.AreEqual(2.3, obj1.Objet3List[1].Value3);
            Assert.IsNotNull(obj1.Objet3List[1].Objet2);
            Assert.AreEqual(2.2, obj1.Objet3List[1].Objet2.Value2);
        }

        [TestMethod]
        public void LazyLoading()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var obj1 = GetQuery<Object1Bis>()
                .EagerLoad(o => o.Objet3List.First().Objet2)
                .LazyLoad(o => o.Objet3List)
                .LazyLoad(o => o.Objet2)
                .First(o => o.Id == 1);

            Assert.IsNotNull(obj1);
            Assert.AreEqual("obj1", obj1.Name);
            Assert.IsNotNull(obj1.Objet2);
            Assert.AreEqual(1.2, obj1.Objet2.Value2);
            Assert.IsNotNull(obj1.Objet3List);
            Assert.AreEqual(2, obj1.Objet3List.Count);

            Assert.AreEqual(1.3, obj1.Objet3List[0].Value3);
            Assert.IsNotNull(obj1.Objet3List[0].Objet2);
            Assert.AreEqual(3.2, obj1.Objet3List[0].Objet2.Value2);

            Assert.AreEqual(2.3, obj1.Objet3List[1].Value3);
            Assert.IsNotNull(obj1.Objet3List[1].Objet2);
            Assert.AreEqual(2.2, obj1.Objet3List[1].Objet2.Value2);
        }

        [TestMethod]
        public void BatchLazyLoading()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var objs = GetQuery<Object1Bis>()
                .BatchLazyLoad(o => o.Objet3List)
                .BatchLazyLoad(o => o.Objet2)
                .Where(o => o.Id == 1 || o.Id == 2).ToArray();

            Assert.IsNotNull(objs);
            Assert.AreEqual(2, objs.Length);

            var obj1 = objs.First(o => o.Id == 1);
            Assert.IsNotNull(obj1.Objet2);
            Assert.AreEqual(1.2, obj1.Objet2.Value2);
            Assert.IsNotNull(obj1.Objet3List);
            Assert.AreEqual(2, obj1.Objet3List.Count);

            var obj2 = objs.First(o => o.Id == 2);
            Assert.IsNotNull(obj2.Objet2);
            Assert.AreEqual(2.2, obj2.Objet2.Value2);
            Assert.IsNotNull(obj2.Objet3List);
            Assert.AreEqual(1, obj2.Objet3List.Count);
        }

        [TestMethod]
        public void DontLoad()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var obj1 = GetQuery<Object1Bis>()
                .EagerLoad(o => o.Objet3List.First().Objet2)
                .LazyLoad(o => o.Objet3List)
                .LazyLoad(o => o.Objet2)
                .DontLoad(o => o.Objet2)
                .DontLoad(o => o.Objet3List.First().Objet2)
                .First(o => o.Id == 1);

            Assert.IsNotNull(obj1);
            Assert.AreEqual("obj1", obj1.Name);
            Assert.IsNull(obj1.Objet2);
            Assert.IsNotNull(obj1.Objet3List);
            Assert.AreEqual(2, obj1.Objet3List.Count);

            Assert.AreEqual(1.3, obj1.Objet3List[0].Value3);
            Assert.IsNull(obj1.Objet3List[0].Objet2);

            Assert.AreEqual(2.3, obj1.Objet3List[1].Value3);
            Assert.IsNull(obj1.Objet3List[1].Objet2);
        }

        [TestMethod]
        public void GroupBy()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var count = GetQuery<Object1>().GroupBy(o => o.Objet2.Value2).Count();
            Assert.AreEqual(3, count);

            var countGroup = GetQuery<Object1>().GroupBy(o => o.Objet2.Value2).Select(g => new { Count = g.Count(), Value = g.Key }).ToList();
            Assert.AreEqual(3, countGroup.Count);
            Assert.IsFalse(countGroup.Any(c => c.Count != 1));
            Assert.IsTrue(countGroup.Any(c => c.Value == 1.2));
            Assert.IsTrue(countGroup.Any(c => c.Value == 2.2));
            Assert.IsTrue(countGroup.Any(c => c.Value == 3.2));
        }

        [TestMethod]
        public void GroupByHaving()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var countGroup = GetQuery<Object1>().GroupBy(o => o.Objet2.Value2).Where(g => g.Key > 2).Select(g => new { Count = g.Count(), Value = g.Key }).ToList();
            Assert.AreEqual(2, countGroup.Count);
            Assert.IsFalse(countGroup.Any(c => c.Count != 1));
            Assert.IsTrue(countGroup.Any(c => c.Value == 2.2));
            Assert.IsTrue(countGroup.Any(c => c.Value == 3.2));
        }

        [TestMethod]
        public void ProjectAnonymousTypes()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var result1 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new { Name = o.Name }).First();
            Assert.IsNotNull(result1);
            Assert.AreEqual("obj1", result1.Name);

            var result2 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new { Value2 = o.Objet2.Value2 }).First();
            Assert.IsNotNull(result2);
            Assert.AreEqual(1.2, result2.Value2);

            var result3 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new { Name = o.Name, Obj2Count = GetQuery<Object2>().Count() }).First();
            Assert.IsNotNull(result3);
            Assert.AreEqual("obj1", result3.Name);
            Assert.AreEqual(3, result3.Obj2Count);

            var result4 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new { Name = o.Name, Obj2Count = GetQuery<Object2>().Where(o2 => o.Objet2.Id == o2.Id).Count() }).First();
            Assert.IsNotNull(result4);
            Assert.AreEqual("obj1", result4.Name);
            Assert.AreEqual(1, result4.Obj2Count);
        }

        [TestMethod]
        public void ProjectOnNonEntityTypes()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var result1 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new ObjName { Name = o.Name }).First();
            Assert.IsNotNull(result1);
            Assert.AreEqual("obj1", result1.Name);

            var result2 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new ObjValue2 { Value2 = o.Objet2.Value2 }).First();
            Assert.IsNotNull(result2);
            Assert.AreEqual(1.2, result2.Value2);

            var result3 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new ObjNameCount { Name = o.Name, Obj2Count = GetQuery<Object2>().Count() }).First();
            Assert.IsNotNull(result3);
            Assert.AreEqual("obj1", result3.Name);
            Assert.AreEqual(3, result3.Obj2Count);

            var result4 = GetQuery<Object1>().Where(o => o.Id == 1).Select(o => new ObjNameCount { Name = o.Name, Obj2Count = GetQuery<Object2>().Where(o2 => o.Objet2.Id == o2.Id).Count() }).First();
            Assert.IsNotNull(result4);
            Assert.AreEqual("obj1", result4.Name);
            Assert.AreEqual(1, result4.Obj2Count);
        }

        [TestMethod]
        public void InheritanceWithOneTablePerClass()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var parent = new ParentObject()
            {
                ParentValue = 5
            };

            var child = new ChildObject()
            {
                ChildValue = 2,
                ParentValue = 3
            };

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Save(parent);
            uw.Save(child);
            uw.Commit();

            parent = GetQuery<ParentObject>().Where(o => o.Id == 1).FirstOrDefault();
            Assert.IsNotNull(parent);
            Assert.AreEqual(5, parent.ParentValue);

            child = GetQuery<ChildObject>().Where(o => o.Id == 1).FirstOrDefault();
            Assert.IsNotNull(child);
            Assert.AreEqual(2, child.ChildValue);
            Assert.AreEqual(3, child.ParentValue);
        }

        [TestMethod]
        public void DbValueType()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var obj = new ObjectWithDbValueType()
            {
                Value = new DbValueTypeTest(5)
            };

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Save(obj);
            uw.Commit();

            obj = GetQuery<ObjectWithDbValueType>().Where(o => o.Id == 1).FirstOrDefault();
            Assert.IsNotNull(obj);
            Assert.IsNotNull(obj.Value);
            Assert.AreEqual(5, obj.Value.GetDbValue());
        }

        public class ObjName
        {
            public string Name { get; set; }
        }

        public class ObjValue2
        {
            public double Value2 { get; set; }
        }

        public class ObjNameCount
        {
            public string Name { get; set; }

            public int Obj2Count { get; set; }
        }

        [TestMethod, Ignore]
        public void GroupByHavingAfterProjection()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var countGroup = GetQuery<Object1>().GroupBy(o => o.Objet2.Value2).Select(g => new { Count = g.Count(), Value = g.Key }).Where(g => g.Count == 2).ToList();
            Assert.AreEqual(0, countGroup.Count);

            countGroup = GetQuery<Object1>().GroupBy(o => o.Objet2.Value2).Select(g => new { Count = g.Count(), Value = g.Key }).Where(g => g.Value > 2).ToList();
            Assert.AreEqual(2, countGroup.Count);
            Assert.IsFalse(countGroup.Any(c => c.Count != 1));
            Assert.IsTrue(countGroup.Any(c => c.Value == 2.2));
            Assert.IsTrue(countGroup.Any(c => c.Value == 3.2));
        }

        private string GetValue()
        {
            return "test2";
        }

        
        public abstract void CreateTables();

        public abstract void DropTables();

        protected void ExecuteNonQuery(string commandText)
        {
            var connection = Provider.GetConnection();
            var cmd = connection.CreateCommand();
            try
            {
                if (cmd.Connection.State != System.Data.ConnectionState.Open) cmd.Connection.Open();
                cmd.CommandText = commandText;
                cmd.ExecuteNonQuery();
            }
            finally
            {
                cmd.Dispose();
                connection.Close();
            }
        }

        public class Object1
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public int? NullNumber { get; set; }

            public Object2 Objet2 { get; set; }

            public List<Object3> Objet3List { get; set; }
        }

        public class Object1Bis
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public Object2 Objet2 { get; set; }

            public IList<Object3> Objet3List { get; set; }
        }

        public class Object2
        {
            public virtual int Id { get; set; }

            public virtual double Value2 { get; set; }
        }

        public class Object3
        {
            public int Id { get; set; }

            public double Value3 { get; set; }

            public Object2 Objet2 { get; set; }
        }

        public class ParentObject
        {
            public int Id { get; set; }

            public int ParentValue { get; set; }
        }

        public class ChildObject : ParentObject
        {
            public int ChildValue { get; set; }
        }

        public class ObjectWithDbValueType
        {
            public int Id { get; set; }

            public DbValueTypeTest Value { get; set; }
        }

        public class DbValueTypeTest : IDbValueType<int>
        {
            private int _value;

            public DbValueTypeTest(int value)
            {
                _value = value;
            }

            public int GetDbValue()
            {
                return _value;
            }
        }
    }
}
