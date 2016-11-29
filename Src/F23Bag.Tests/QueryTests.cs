using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using F23Bag.Data;
using F23Bag.Data.DML;
using System.Text.RegularExpressions;
using System.Data;
using F23Bag.Data.DDL;
using System.Reflection;

namespace F23Bag.Tests
{
    [TestClass]
    public class QueryTests
    {
        [TestMethod]
        public void QueryToAst_SubRequest()
        {
            Assert.AreEqual(REL(@"
            { request skip 1 take 2
                select { column { alias { identifier OBJECT1 } } . { identifier ID } } { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                where { And : 
                    { NotEqual : 
                        { column { alias { identifier OBJECT1 } } . { identifier NAME } } : 
                        { constant null } } : 
                    { Equal : 
                        { request skip 0 take 0 
                            select { Count } 
                            where { Equal : 
                                { column { alias { identifier OBJECT1 } } . { identifier ID } } : 
                                { column { alias { identifier OBJECT2 } } . { identifier IDFK_OBJECTS } } } : 
                        { constant 4 } } }
                 order { column { alias { identifier OBJECT1 } } . { identifier ID } } asc"), 
                 GetRequest<Object1>(q =>
                    q.Skip(1).Take(2)
                    .Where(o => o.Name != null && o.Objects.Count() == 4)));
        }

        [TestMethod]
        public void QueryToAst_InnerJoin()
        {
            Assert.AreEqual(REL(@"
            { request skip 1 take 2 
                select { column { alias { identifier OBJECT1 } } . { identifier ID } } { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
	            { Inner join { alias { identifier OBJECT2 } } on 
		            { Equal : 
			            { column { alias { identifier OBJECT1 } } . { identifier ID } } : 
			            { column { alias { identifier OBJECT2 } } . { identifier IDFK_OBJECTS } } } }
	            where { And : 
		            { NotEqual : 
			            { column { alias { identifier OBJECT1 } } . { identifier NAME } } : 
			            { constant null } } : 
		            { GreaterThan : 
			            { column { alias { identifier OBJECT2 } } . { identifier VALUE } } : 
			            { constant 5 } } }
                order { column { alias { identifier OBJECT1 } } . { identifier ID } } asc"), 
                GetRequest<Object1>(q =>
                    q.Skip(1).Take(2)
                    .Where(o => o.Name != null && o.Objects.Any(o2 => o2.Value > 5))));
        }

        [TestMethod]
        public void QueryToAst_JoinAndSubRequest()
        {
            Assert.AreEqual(REL(@"
            { request skip 1 take 2 
                select { column { alias { identifier OBJECT1 } } . { identifier ID } } { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
	            { Inner join { alias { identifier OBJECT2 } } on 
       		            { Equal : 
	        	            { column { alias { identifier OBJECT1 } } . { identifier ID } } : 
	        	            { column { alias { identifier OBJECT2 } } . { identifier IDFK_OBJECTS } } } }
	            where { And : 
		            { And : 
			            { NotEqual : 
				            { column { alias { identifier OBJECT1 } } . { identifier NAME } } : 
				            { constant null } } : 
			            { GreaterThan : 
				            { column { alias { identifier OBJECT2 } } . { identifier VALUE } } : 
				            { constant 5 } } } : 
		            { Equal : 
			            { request skip 0 take 0 
				            select { Count } 
				            where { Equal : 
					            { column { alias { identifier OBJECT1 } } . { identifier ID } } : 
					            { column { alias { identifier OBJECT2 } } . { identifier IDFK_OBJECTS } } } : 
			            { constant 4 } } }
                order { column { alias { identifier OBJECT1 } } . { identifier ID } } asc"), 
                GetRequest<Object1>(q =>
                    q.Skip(1).Take(2)
                    .Where(o => o.Name != null && o.Objects.Any(o2 => o2.Value > 5) && o.Objects.Count() == 4)));
        }

        [TestMethod]
        public void QueryToAst_SelectWithAnonymousType()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
	            select { column { alias { identifier OBJECT1 } } . { identifier NAME } }"), 
            GetRequest<Object1>(q => q.Select(o => new { Name = o.Name })));
        }

        [TestMethod]
        public void QueryToAst_SelectProperty()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { alias { identifier OBJECT2 } }
                        { Inner join { alias { identifier OBJECT2 } }
                            on { Equal :
                                    { column { alias { identifier OBJECT2 } } . { identifier ID } } : 
		                            { column { alias { identifier OBJECT1 } } . { identifier IDFK_OBJECT } } } }"), 
            GetRequest<Object1>(q => q.Select(o => o.Object)));
        }

        [TestMethod]
        public void QueryToAst_SelectMany()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { alias { identifier OBJECT2 } }
                    { Inner join { alias { identifier OBJECT2 } } 
                        on { Equal : 
                            { column { alias { identifier OBJECT1 } } . { identifier ID } } : 
                            { column { alias { identifier OBJECT2 } } . { identifier IDFK_OBJECTS } } } }"), 
            GetRequest<Object1>(q => q.SelectMany(o => o.Objects)));
        }

        [TestMethod]
        public void QueryToAst_GroupByOneColumn()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier NAME } }
                group by 
                    { column { alias { identifier OBJECT1 } } . { identifier NAME } }"), 
            GetRequest<Object1>(q => q.GroupBy(o => o.Name)));
        }

        [TestMethod]
        public void QueryToAst_GroupByMultipleColumns()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                group by 
                    { column { alias { identifier OBJECT1 } } . { identifier NAME } } 
                    { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }"), 
            GetRequest<Object1>(q => q.GroupBy(o => new { o.Name, o.FullDescription })));
        }

        [TestMethod]
        public void QueryToAst_CustomExpressionToSqlAst()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                group by 
                    { constant converter! } 
                    { constant converter! }"),
            GetRequest<Object1>(q => q.GroupBy(o => new { o.Name, o.FullDescription }), new ExpressionConverter()));
        }

        [TestMethod]
        public void QueryToAst_CustomPropertyMapper()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier NameCustomMapper } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                group by 
                    { column { alias { identifier OBJECT1 } } . { identifier NAME } } 
                    { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }"), 
             GetRequest<Object1>(q => q.GroupBy(o => new { o.Name, o.FullDescription }), new[] { new PropertyMapper() }));
        }

        [TestMethod]
        public void QueryToAst_GroupByHaving()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                group by 
                    { column { alias { identifier OBJECT1 } } . { identifier NAME } } 
                    { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } } 
                having 
                    { GreaterThan : 
                        { Count } : 
                        { constant 1 } }"), 
            GetRequest<Object1>(q => 
                q.GroupBy(o => new { o.Name, o.FullDescription })
                .Where(g => g.Count() > 1)));
        }

        [TestMethod]
        public void QueryToAst_Contains()
        {
            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier ID } } { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                where 
                    { in { constant 1 } { constant 2 } { constant 3 } { constant 4 } }"), 
            GetRequest<Object1>(q => q.Where(o => new List<int>() { 1, 2, 3, 4 }.Contains(o.Id))));

            Assert.AreEqual(REL(@"
            { request skip 0 take 0 
                select { column { alias { identifier OBJECT1 } } . { identifier ID } } { column { alias { identifier OBJECT1 } } . { identifier NAME } } { column { alias { identifier OBJECT1 } } . { identifier FULL_DESCRIPTION } }
                where 
                    { in { constant 5 } { constant 6 } { constant 7 } }"), 
            GetRequest<Object1>(q => q.Where(o => new int[] { 5, 6, 7 }.Contains(o.Id))));
        }

        private string GetRequest<T>(Func<IQueryable<T>, object> defineQuery, params IExpresstionToSqlAst[] converters)
        {
            return GetRequest(defineQuery, null, converters);
        }

        private string GetRequest<T>(Func<IQueryable<T>, object> defineQuery, IPropertyMapper[] mappers, params IExpresstionToSqlAst[] converters)
        {
            var translator = new FakeSqlTraductor();
            defineQuery(new Query<T>(new DbQueryProvider(new FakeSqlProvider(translator), new DefaultSqlMapping(mappers), converters, null))).ToString();
            return translator.Request.ToString();
        }

        private string REL(string str)
        {
            return new Regex("\\s+").Replace(str, m => " ").Trim();
        }

        private class ExpressionConverter : IExpresstionToSqlAst
        {
            public bool Accept(System.Linq.Expressions.Expression expression)
            {
                return expression is System.Linq.Expressions.MemberExpression;
            }

            public DMLNode Convert(System.Linq.Expressions.Expression expression, Request request, ISQLMapping sqlMapping)
            {
                return new Constant("converter!");
            }
        }

        private class PropertyMapper : IPropertyMapper
        {
            public bool Accept(PropertyInfo property)
            {
                return property.Name == "Name";
            }

            public SelectInfo DeclareMap(Request request, PropertyInfo property, AliasDefinition alias)
            {
                return new SelectInfo(new ColumnAccess(alias, new Identifier("NameCustomMapper")), property);
            }

            public void Map(object o, PropertyInfo property, IDataRecord reader, int readerIndex)
            {
                throw new NotImplementedException();
            }
        }

        private class FakeSqlTraductor : ISQLTranslator
        {
            public string Translate(Request request, ICollection<Tuple<string, object>> parameters)
            {
                Request = request;
                Parameters = parameters;
                return null;
            }

            public int GetRank(IDataRecord record)
            {
                return 0;
            }

            public IEnumerable<string> Translate(DDLStatement ddlStatement, ISQLMapping sqlMapping)
            {
                throw new NotImplementedException();
            }

            public Request Request { get; private set; }

            public ICollection<Tuple<string, object>> Parameters { get; private set; }
        }

        private class FakeSqlProvider : ISQLProvider
        {
            private readonly ISQLTranslator _translator;

            public FakeSqlProvider(ISQLTranslator translator)
            {
                _translator = translator;
            }

            public IDbCommand CreateCommand()
            {
                throw new NotImplementedException();
            }

            public IDbCommand CreateCommand(Request request)
            {
                throw new NotImplementedException();
            }

            public IDbConnection GetConnection()
            {
                throw new NotImplementedException();
            }

            public IDDLTranslator GetDDLTranslator()
            {
                throw new NotImplementedException();
            }

            public IQueryable<T> GetQuery<T>()
            {
                throw new NotImplementedException();
            }

            public ISQLTranslator GetSQLTranslator()
            {
                return _translator;
            }
        }

        private class Object1
        {
            public int Id { get; set; }

            public string Name { get; set; }

            public string FullDescription { get; set; }

            public Object2 Object { get; set; }

            public List<Object2> Objects { get; set; }
        }

        private class Object2
        {
            public int Id { get; set; }

            public decimal Value { get; set; }
        }
    }
}
