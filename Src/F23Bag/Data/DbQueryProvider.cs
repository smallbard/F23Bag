using System;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using F23Bag.Data.DML;
using F23Bag.Data.Mapping;

namespace F23Bag.Data
{
    public class DbQueryProvider : QueryProvider
    {
        private readonly ISQLProvider _sqlProvider;
        private readonly ISQLMapping _sqlMapping;
        private readonly IEnumerable<IExpresstionToSqlAst> _customConverters;
        private readonly Func<Type, object> _resolver;

        public DbQueryProvider(ISQLProvider sqlProvider, ISQLMapping sqlMapping, IEnumerable<IExpresstionToSqlAst> customConverters, Func<Type, object> resolver)
        {
            _sqlProvider = sqlProvider;
            _sqlMapping = sqlMapping;
            _customConverters = customConverters;
            _resolver = resolver;
        }

        public ISQLMapping SqlMapping { get { return _sqlMapping; } }

        public override string GetQueryText(Expression expression)
        {
            var request = new ExpressionToSqlAst(_sqlMapping, _customConverters, new Mapper(this));
            return _sqlProvider.GetSQLTranslator().Translate(request.Translate(Evaluator.PartialEval(expression)), new List<Tuple<string, object>>());
        }

        public override object Execute(Expression expression)
        {
            var mapper = new Mapper(this);
            return Execute(mapper, new ExpressionToSqlAst(_sqlMapping, _customConverters, mapper).Translate(Evaluator.PartialEval(expression)), expression.Type);
        }

        internal object Execute(Mapper mapper, Request request, Type expressionType)
        {
            var parameters = new List<Tuple<string, object>>();
            var commandText = _sqlProvider.GetSQLTranslator().Translate(request, parameters);

            var connection = _sqlProvider.GetConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;

            foreach (var parameter in parameters)
            {
                var dbParameter = cmd.CreateParameter();
                dbParameter.ParameterName = parameter.Item1;
                dbParameter.Value = parameter.Item2;
                cmd.Parameters.Add(dbParameter);
            }

            if (typeof(System.Collections.IEnumerable).IsAssignableFrom(expressionType) && expressionType != typeof(string))
                return typeof(DbQueryProvider).GetMethod("GetResults", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(expressionType.GetGenericArguments()[0]).Invoke(this, new object[] { connection, cmd, cmd.ExecuteReader(), request, mapper });
            else if (expressionType.IsClass && expressionType != typeof(string))
                return typeof(DbQueryProvider).GetMethod("GetFirstResult", BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(expressionType).Invoke(this, new object[] { connection, cmd, cmd.ExecuteReader(), request, mapper });
            else
                try
                {
                    return cmd.ExecuteScalar();
                }
                finally
                {
                    cmd.Dispose();
                    connection.Dispose();
                }
        }

        private T GetFirstResult<T>(DbConnection connection, DbCommand command, DbDataReader reader, Request request, Mapper mapper)
        {
            return new ObjectReader<T>(connection, command, reader, request, _sqlProvider.GetSQLTranslator(), mapper, _resolver).FirstOrDefault();
        }

        private IEnumerable<T> GetResults<T>(DbConnection connection, DbCommand command, DbDataReader reader, Request request, Mapper mapper)
        {
            return new ObjectReader<T>(connection, command, reader, request, _sqlProvider.GetSQLTranslator(), mapper, _resolver);
        }
    }
}
