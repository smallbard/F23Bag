using Microsoft.VisualStudio.TestTools.UnitTesting;
using System.Collections.Generic;
using System.Linq;
using F23Bag.Data;

namespace F23Bag.Tests
{
    public abstract class DatabaseProviderTests
    {
        protected abstract ISQLProvider Provider { get; }

        protected abstract bool EnableTests { get; }

        public IQueryable<T> GetQuery<T>()
        {
            return new Query<T>(new DbQueryProvider(Provider, new DefaultSqlMapping(null), null, null));
        }

        [TestMethod]
        public void SimpleQueries()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            Assert.AreEqual(3, GetQuery<Objet1>().Where(o => o.Name != null).Count());

            var objs = GetQuery<Objet1>().Where(o => ((o.Id > 1 && o.Id < 10) || (o.Id >= 2 && o.Id <= 9)) && o.Id != 0).OrderBy(o => o.Id).ThenBy(o => o.Name).ToList();
            Assert.AreEqual(2, objs.Count);
            Assert.AreEqual(2, objs[0].Id);
            Assert.AreEqual("obj2", objs[0].Name);
            Assert.AreEqual("obj3", objs[1].Name);

            Assert.AreEqual(1, GetQuery<Objet1>().Min(o => o.Id));
            Assert.AreEqual(3, GetQuery<Objet1>().Max(o => o.Id));
            Assert.AreEqual(2, GetQuery<Objet1>().Average(o => o.Id));

            Assert.AreEqual(1, GetQuery<Objet1>().Where(o => o.Name.Contains("j2")).Count());
            Assert.AreEqual(3, GetQuery<Objet1>().Where(o => o.Name.StartsWith("ob")).Count());
            Assert.AreEqual(1, GetQuery<Objet1>().Where(o => o.Name.EndsWith("j3")).Count());
        }

        [TestMethod]
        public void UnitOfWork_UdapteInsert()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));

            uw.Update(GetQuery<Objet1>().Where(o => o.Id == 2), o => new Objet1() { Name = "testUW" });
            uw.Insert(GetQuery<Objet1>().Where(o => o.Id != 2), o => new Objet1() { Name = o.Name + "bis" });
            uw.Commit();

            Assert.AreEqual("testUW", GetQuery<Objet1>().First(o => o.Id == 2).Name);
            Assert.AreEqual(5, GetQuery<Objet1>().Count());
        }

        [TestMethod]
        public void UnitOfWork_DeleteExecute()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Delete(GetQuery<Objet3>());
            uw.Delete(GetQuery<Objet1>().Where(o => o.Id == 2));
            uw.Delete(GetQuery<Objet1>().First(o => o.Id == 1));
            uw.Execute("UPDATE OBJET1 SET NAME = 'testUWCommand' WHERE ID > 1", null);
            uw.Commit();

            Assert.AreEqual(1, GetQuery<Objet1>().Count());
            Assert.AreEqual(1, GetQuery<Objet1>().Where(o => o.Name == "testUWCommand").Count());
        }

        [TestMethod]
        public void UnitOfWork_Save()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var newObj = new Objet1() { Name = "createdByUw", Objet2 = new Objet2() { Value2 = 77 } };
            newObj.Objet3List = new List<Objet3>() { new Objet3() { Value3 = 88 }, new Objet3() { Value3 = 99 } };

            var uw = new UnitOfWork(Provider, new DefaultSqlMapping(null));
            uw.Save(newObj);
            uw.Commit();

            Assert.AreNotEqual(0, newObj.Id);

            var readObj = GetQuery<Objet1>().EagerLoad(o => o.Objet2).EagerLoad(o => o.Objet3List).First(o => o.Id == newObj.Id);
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
        public void QueriesWithJoin()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var objs = GetQuery<Objet1>().Where(o => o.Objet3List.Any(o3 => o3.Value3 == 1.3)).ToList();
            Assert.AreEqual(1, objs.Count);

            var sum = GetQuery<Objet1>().Where(o => o.Id == 1).SelectMany(o => o.Objet3List).Sum(o3 => o3.Value3);

            objs = GetQuery<Objet1>().Where(o => o.Objet3List.Sum(o3 => o3.Value3) == sum).ToList();
            Assert.AreEqual(1, objs.Count);
        }

        [TestMethod]
        public void EagerLoading()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var obj1 = GetQuery<Objet1>()
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

            var obj1 = GetQuery<Objet1Bis>()
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
        public void DontLoad()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var obj1 = GetQuery<Objet1Bis>()
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

            var count = GetQuery<Objet1>().GroupBy(o => o.Objet2.Value2).Count();
            Assert.AreEqual(3, count);

            var countGroup = GetQuery<Objet1>().GroupBy(o => o.Objet2.Value2).Select(g => new { Count = g.Count(), Value = g.Key }).ToList();
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

            var countGroup = GetQuery<Objet1>().GroupBy(o => o.Objet2.Value2).Where(g => g.Key > 2).Select(g => new { Count = g.Count(), Value = g.Key }).ToList();
            Assert.AreEqual(2, countGroup.Count);
            Assert.IsFalse(countGroup.Any(c => c.Count != 1));
            Assert.IsTrue(countGroup.Any(c => c.Value == 2.2));
            Assert.IsTrue(countGroup.Any(c => c.Value == 3.2));
        }

        [TestMethod, Ignore]
        public void GroupByHavingAfterProjection()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");

            var countGroup = GetQuery<Objet1>().GroupBy(o => o.Objet2.Value2).Select(g => new { Count = g.Count(), Value = g.Key }).Where(g => g.Count == 2).ToList();
            Assert.AreEqual(0, countGroup.Count);

            countGroup = GetQuery<Objet1>().GroupBy(o => o.Objet2.Value2).Select(g => new { Count = g.Count(), Value = g.Key }).Where(g => g.Value > 2).ToList();
            Assert.AreEqual(2, countGroup.Count);
            Assert.IsFalse(countGroup.Any(c => c.Count != 1));
            Assert.IsTrue(countGroup.Any(c => c.Value == 2.2));
            Assert.IsTrue(countGroup.Any(c => c.Value == 3.2));
        }

        [TestMethod, Ignore]
        public void RecursiveRequest()
        {
            if (!EnableTests) Assert.Inconclusive("Configure the connection string in app.Config.");
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

        public class Objet1
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public Objet2 Objet2 { get; set; }

            public List<Objet3> Objet3List { get; set; }
        }

        public class Objet1Bis
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public Objet2 Objet2 { get; set; }

            public IList<Objet3> Objet3List { get; set; }
        }

        public class Objet2
        {
            public virtual int Id { get; set; }

            public virtual double Value2 { get; set; }
        }

        public class Objet3
        {
            public int Id { get; set; }

            public double Value3 { get; set; }

            public Objet2 Objet2 { get; set; }
        }
    }
}
