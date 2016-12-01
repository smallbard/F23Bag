using F23Bag.Data.DML;
using F23Bag.Domain;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data
{
    public class UnitOfWork
    {
        private readonly ISQLProvider _sqlProvider;
        private readonly ISQLMapping _sqlMapping;
        private readonly List<Action> _operations;
        private readonly HashSet<object> _alreadySaved;

        public UnitOfWork(ISQLProvider sqlProvider, ISQLMapping sqlMapping)
        {
            _sqlProvider = sqlProvider;
            _sqlMapping = sqlMapping;
            _operations = new List<Action>();
            _alreadySaved = new HashSet<object>();
        }

        public void Save(object o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            _operations.Add(() =>
            {
                Save(o, null, null);

                foreach (var property in o.GetType().GetProperties())
                    if (property.PropertyType.IsClass && property.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType))
                    {
                        var value = property.GetValue(o);
                        if (value == null) continue;
                        var idProperty = o.GetType().GetProperty("Id");
                        foreach (var obj in (System.Collections.IEnumerable)value) Save(obj, property, idProperty.GetValue(o));
                    }
            });
        }

        public void Delete(object o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            _operations.Add(() =>
            {
                var idProperty = o.GetType().GetProperty("Id");

                var fromAlias = new AliasDefinition(_sqlMapping.GetSqlEquivalent(o.GetType()));
                var request = new Request()
                {
                    FromAlias = fromAlias,
                    RequestType = RequestType.Delete
                };
                request.Where = new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal, _sqlMapping.GetSqlEquivalent(request, fromAlias, idProperty, false), new Constant(idProperty.GetValue(o)));

                var parameters = new List<Tuple<string, object>>();
                var sql = _sqlProvider.GetSQLTranslator().Translate(request, parameters);
                using (var connection = _sqlProvider.GetConnection())
                {
                    if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                    using (var cmd = connection.CreateCommand())
                    {
                        cmd.CommandText = sql;
                        foreach (var parameter in parameters)
                        {
                            var dbParameter = cmd.CreateParameter();
                            dbParameter.ParameterName = parameter.Item1;
                            dbParameter.Value = parameter.Item2;
                            cmd.Parameters.Add(dbParameter);
                        }
                        cmd.ExecuteNonQuery();
                    }
                }
            });
        }

        public void Delete<TSource>(IQueryable<TSource> source)
        {
            _operations.Add(() => source.Provider.Execute<int>(Expression.Call(Expression.Constant(this), new Action<IQueryable<TSource>>(Delete).Method, source.Expression)));
        }

        public void Update<TSource>(IQueryable<TSource> source, Expression<Func<TSource, TSource>> updateExpression)
        {
            _operations.Add(() => source.Provider.Execute<int>(Expression.Call(Expression.Constant(this), new Action<IQueryable<TSource>, Expression<Func<TSource, TSource>>>(Update).Method, source.Expression, Expression.Quote(updateExpression))));
        }

        public void Insert<TSource, TInsert>(IQueryable<TSource> source, Expression<Func<TSource, TInsert>> insertExpression)
        {
            _operations.Add(() => source.Provider.Execute<int>(Expression.Call(Expression.Constant(this), new Action<IQueryable<TSource>, Expression<Func<TSource, TInsert>>>(Insert).Method, source.Expression, Expression.Quote(insertExpression))));
        }

        public void Execute(string sql, IEnumerable<Tuple<string, object>> parameters)
        {
            _operations.Add(() =>
            {
                using (var connection = _sqlProvider.GetConnection())
                using (var cmd = connection.CreateCommand())
                {
                    if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                    cmd.CommandText = sql;

                    if (parameters != null)
                        foreach (var param in parameters)
                        {
                            var parameter = cmd.CreateParameter();
                            parameter.ParameterName = param.Item1;
                            parameter.Value = param.Item2;
                            cmd.Parameters.Add(parameter);
                        }

                    cmd.ExecuteNonQuery();
                }
            });
        }

        public void Commit()
        {
            _operations.ForEach(op => op());
            _operations.Clear();
            _alreadySaved.Clear();
        }

        public void Rollback()
        {
            _operations.Clear();
            _alreadySaved.Clear();
        }

        private void Save(object o, PropertyInfo parentProperty, object parentId)
        {
            // no infinite loop
            if (_alreadySaved.Contains(o)) return;
            _alreadySaved.Add(o);

            var idProperty = o.GetType().GetProperty("Id");
            var id = idProperty.GetValue(o);

            var doInsert = (idProperty.PropertyType.IsValueType && Activator.CreateInstance(idProperty.PropertyType).Equals(id)) || (!idProperty.PropertyType.IsValueType && id == null);

            var fromAlias = new AliasDefinition(_sqlMapping.GetSqlEquivalent(o.GetType()));
            var request = new Request()
            {
                FromAlias = fromAlias,
                RequestType = doInsert ? RequestType.InsertValues : RequestType.Update,
                IdColumnName = _sqlMapping.GetColumnName(idProperty)
            };

            foreach (var property in o.GetType().GetProperties().Where(p => p.GetCustomAttribute<TransientAttribute>() == null))
            {
                if (property.Name == "Id") continue;
                var value = property.GetValue(o);

                if (property.PropertyType.IsClass && property.PropertyType != typeof(string))
                {
                    if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType)) continue;

                    if (value != null)
                    {
                        Save(value, null, null);
                        value = value.GetType().GetProperty("Id").GetValue(value);
                    }
                }

                request.UpdateOrInsert.Add(new UpdateOrInsertInfo(new Constant(value), new ColumnAccess(fromAlias, new Identifier(_sqlMapping.GetColumnName(property)))));
            }

            if (!doInsert) request.Where = new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal, _sqlMapping.GetSqlEquivalent(request, fromAlias, idProperty, false), new Constant(id));

            if (parentProperty != null && parentId != null)
            {
                var columnName = _sqlMapping.GetColumnName(parentProperty);
                foreach (var uoi in request.UpdateOrInsert)
                    if (uoi.Destination.Column.IdentifierName == columnName)
                    {
                        request.UpdateOrInsert.Remove(uoi);
                        break;
                    }

                request.UpdateOrInsert.Add(new UpdateOrInsertInfo(new Constant(parentId), new ColumnAccess(fromAlias, new Identifier(columnName))));
            }

            var parameters = new List<Tuple<string, object>>();
            var sql = _sqlProvider.GetSQLTranslator().Translate(request, parameters);
            using (var connection = _sqlProvider.GetConnection())
            {
                if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                using (var cmd = connection.CreateCommand())
                {
                    cmd.CommandText = sql;
                    foreach (var parameter in parameters)
                    {
                        var dbParameter = cmd.CreateParameter();
                        dbParameter.ParameterName = parameter.Item1;
                        dbParameter.Value = parameter.Item2;
                        cmd.Parameters.Add(dbParameter);
                    }

                    if (doInsert)
                        idProperty.SetValue(o, Convert.ChangeType(cmd.ExecuteScalar(), idProperty.PropertyType));
                    else
                        cmd.ExecuteNonQuery();
                }
            }
        }
    }
}
