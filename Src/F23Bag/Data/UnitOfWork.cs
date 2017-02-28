using F23Bag.Data.DML;
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
            _operations.Add(() => ((DoSave)Activator.CreateInstance(typeof(DoSave<>).MakeGenericType(o.GetType()))).Save(o, null, null, _alreadySaved, _sqlProvider, _sqlMapping));
        }

        public void Delete(object o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            _operations.Add(() =>
            {
                var idProperty = _sqlMapping.GetIdProperty(o.GetType());

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

        private abstract class DoSave
        {
            public abstract object GetId(object o);

            public abstract void Save(object o, PropertyInfo parentProperty, object parentId, HashSet<object> alreadySaved, ISQLProvider sqlProvider, ISQLMapping sqlMapping);
        }

        private class DoSave<T> : DoSave
        {
            private static readonly PropertyInfo _idProperty;
            private static readonly Func<object, object> _getId;
            private static readonly Action<object, object> _setId;
            private static readonly IEnumerable<Tuple<PropertyInfo, Func<object, object>>> _properties;

            static DoSave()
            {
                _idProperty = typeof(T).GetProperty("Id");
                var pac = new PropertyAccessorCompiler(_idProperty);
                _getId = pac.GetPropertyValue;
                _setId = pac.SetPropertyValue;
                _properties = typeof(T).GetProperties()
                    .Where(p => p.GetCustomAttribute<TransientAttribute>() == null && p.GetCustomAttribute<InversePropertyAttribute>() == null && p.Name != "Id")
                    .Select(p => Tuple.Create(p, new PropertyAccessorCompiler(p).GetPropertyValue)).ToArray();
            }

            public override object GetId(object o)
            {
                return _getId(o);
            }

            public override void Save(object o, PropertyInfo parentProperty, object parentId, HashSet<object> alreadySaved, ISQLProvider sqlProvider, ISQLMapping sqlMapping)
            {
                // no infinite loop
                if (alreadySaved.Contains(o)) return;
                alreadySaved.Add(o);

                var id = _getId(o);

                var doInsert = (_idProperty.PropertyType.IsValueType && Activator.CreateInstance(_idProperty.PropertyType).Equals(id)) || (!_idProperty.PropertyType.IsValueType && id == null);

                var fromAlias = new AliasDefinition(sqlMapping.GetSqlEquivalent(o.GetType()));
                var request = new Request()
                {
                    FromAlias = fromAlias,
                    RequestType = doInsert ? RequestType.InsertValues : RequestType.Update,
                    IdColumnName = sqlMapping.GetColumnName(_idProperty)
                };

                foreach (var property in _properties)
                {
                    var value = property.Item2(o);

                    if (property.Item1.PropertyType.IsClass && property.Item1.PropertyType != typeof(string))
                    {
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(property.Item1.PropertyType)) continue;

                        if (value != null)
                        {
                            var ds = (DoSave)Activator.CreateInstance(typeof(DoSave<>).MakeGenericType(property.Item1.PropertyType));
                            ds.Save(value, null, null, alreadySaved, sqlProvider, sqlMapping);
                            value = ds.GetId(value);
                        }
                    }

                    request.UpdateOrInsert.Add(new UpdateOrInsertInfo(new Constant(value), new ColumnAccess(fromAlias, new Identifier(sqlMapping.GetColumnName(property.Item1)))));
                }

                if (!doInsert) request.Where = new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal, sqlMapping.GetSqlEquivalent(request, fromAlias, _idProperty, false), new Constant(id));

                if (parentProperty != null && parentId != null)
                {
                    var columnName = sqlMapping.GetColumnName(parentProperty);
                    foreach (var uoi in request.UpdateOrInsert)
                        if (uoi.Destination.Column.IdentifierName == columnName)
                        {
                            request.UpdateOrInsert.Remove(uoi);
                            break;
                        }

                    request.UpdateOrInsert.Add(new UpdateOrInsertInfo(new Constant(parentId), new ColumnAccess(fromAlias, new Identifier(columnName))));
                }

                var parameters = new List<Tuple<string, object>>();
                var sql = sqlProvider.GetSQLTranslator().Translate(request, parameters);
                using (var connection = sqlProvider.GetConnection())
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
                            _setId(o, Convert.ChangeType(id = cmd.ExecuteScalar(), _idProperty.PropertyType));
                        else
                            cmd.ExecuteNonQuery();
                    }
                }

                foreach (var property in _properties)
                    if (property.Item1.PropertyType.IsClass && property.Item1.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.Item1.PropertyType))
                    {
                        var value = property.Item2(o);
                        if (value == null) continue;
                        var doSave = (DoSave)Activator.CreateInstance(typeof(DoSave<>).MakeGenericType(property.Item1.PropertyType.GetGenericArguments()[0]));
                        foreach (var obj in (System.Collections.IEnumerable)value)
                        {
                            doSave.Save(obj, property.Item1, id, alreadySaved, sqlProvider, sqlMapping);
                        }
                    }
            }
        }
    }
}
