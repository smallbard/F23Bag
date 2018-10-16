using System;
using System.Collections.Generic;
using System.Data;
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
        private readonly List<Tuple<string, object>> _parameters;
        
        public DbQueryProvider(ISQLProvider sqlProvider, ISQLMapping sqlMapping, IEnumerable<IExpresstionToSqlAst> customConverters, Func<Type, object> resolve)
        {
            _sqlProvider = sqlProvider;
            _sqlMapping = sqlMapping;
            _customConverters = customConverters;
            _parameters = new List<Tuple<string, object>>();
            Resolve = resolve;
        }

        public static event EventHandler<SqlExecutionEventArgs> SqlExecution;

        public ISQLMapping SqlMapping { get { return _sqlMapping; } }

        public Func<Type, object> Resolve { get; private set; }

        public void AddParameter(string parameterName, object value)
        {
            _parameters.Add(Tuple.Create(parameterName, value));
        }

        public override string GetQueryText(Expression expression)
        {
            var request = new ExpressionToSqlAst(_sqlMapping, _customConverters, new Mapper(this));
            return _sqlProvider.GetSQLTranslator().Translate(request.Translate(Evaluator.PartialEval(expression)), new List<Tuple<string, object>>());
        }

        public override object Execute(Expression expression)
        {
            if (expression == null) throw new ArgumentNullException(nameof(expression));

            var mapper = new Mapper(this);
            return Execute(mapper, new ExpressionToSqlAst(_sqlMapping, _customConverters, mapper).Translate(Evaluator.PartialEval(expression)), expression.Type);
        }

        internal object Execute(Mapper mapper, Request request, Type expressionType)
        {
            var commandText = _sqlProvider.GetSQLTranslator().Translate(request, _parameters);

            var connection = _sqlProvider.GetConnection();
            if (connection.State != System.Data.ConnectionState.Open) connection.Open();

            var cmd = connection.CreateCommand();
            cmd.CommandText = commandText;
            cmd.CommandTimeout = 0;

            foreach (var parameter in _parameters)
            {
                var dbParameter = cmd.CreateParameter();
                dbParameter.ParameterName = parameter.Item1;
                dbParameter.Value = parameter.Item2;
                cmd.Parameters.Add(dbParameter);
            }

            OnSqlExecution(this, commandText, _parameters.ToDictionary(t => t.Item1, t => t.Item2));

            if (expressionType.IsCollection())
                return typeof(DbQueryProvider).GetMethod(nameof(GetResults), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(expressionType.GetGenericArguments()[0]).Invoke(this, new object[] { connection, cmd, cmd.ExecuteReader(), request, mapper });
            else if (expressionType.IsEntityOrCollection())
                return typeof(DbQueryProvider).GetMethod(nameof(GetFirstResult), BindingFlags.NonPublic | BindingFlags.Instance).MakeGenericMethod(expressionType).Invoke(this, new object[] { connection, cmd, cmd.ExecuteReader(), request, mapper });
            else
                try
                {
                    return cmd.ExecuteScalar();
                }
                catch (Exception ex)
                {
                    throw new SQLException($"Error '{ex.Message}' in query : {commandText}", ex);
                }
                finally
                {
                    cmd.Dispose();
                    connection.Dispose();
                }
        }

        protected static void OnSqlExecution(DbQueryProvider provider, string sql, Dictionary<string, object> parameters)
        {
            SqlExecution?.Invoke(provider, new SqlExecutionEventArgs(sql, parameters));
        }

        private T GetFirstResult<T>(IDbConnection connection, IDbCommand command, IDataReader reader, Request request, Mapper mapper)
        {
            return new ObjectReader<T>(connection, command, reader, request, _sqlProvider.GetSQLTranslator(), mapper, Resolve).FirstOrDefault();
        }

        private IEnumerable<T> GetResults<T>(IDbConnection connection, IDbCommand command, IDataReader reader, Request request, Mapper mapper)
        {
            return new ObjectReader<T>(connection, command, reader, request, _sqlProvider.GetSQLTranslator(), mapper, Resolve);
        }
    }
}
