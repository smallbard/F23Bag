﻿using F23Bag.Data.ChangeTracking;
using F23Bag.Data.DML;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data
{
    public class UnitOfWork
    {
        private readonly ISQLProvider _sqlProvider;
        private readonly ISQLMapping _sqlMapping;
        private readonly List<Action<IDbConnection>> _operations;
        private readonly HashSet<object> _alreadySaved;
        private readonly Dictionary<object, State> _extractedStates;

        public UnitOfWork(ISQLProvider sqlProvider, ISQLMapping sqlMapping)
        {
            _sqlProvider = sqlProvider;
            _sqlMapping = sqlMapping;
            _operations = new List<Action<IDbConnection>>();
            _alreadySaved = new HashSet<object>();
            _extractedStates = new Dictionary<object, State>();
        }

        public event EventHandler<UnitOfWorkEventArgs> BeforeDelete;

        public event EventHandler<UnitOfWorkEventArgs> BeforeUpdate;

        public event EventHandler<UnitOfWorkEventArgs> BeforeInsert;

        public void TrackChange(object o)
        {
            var states = StateExtractor.GetStateExtractor(_sqlMapping, o.GetType()).GetAllComponentStates(o);
            foreach (var os in states.Keys)
                _extractedStates[os] = states[os];
        }

        public void Save<T>(T o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));
            _operations.Add(connection =>
            {
                var ds = new DoSave<T>();
                ds.BeforeUpdate += (s, e) => BeforeUpdate?.Invoke(this, e);
                ds.BeforeInsert += (s, e) => BeforeInsert?.Invoke(this, e);
                ds.Save(connection, o, null, null, _alreadySaved, _sqlProvider, _sqlMapping, _extractedStates);
            });
        }

        public void Delete(object o)
        {
            if (o == null) throw new ArgumentNullException(nameof(o));

            _operations.Add(connection =>
            {
                _extractedStates.Remove(o);

                var idProperty = _sqlMapping.GetIdProperty(o.GetType());
                var id = idProperty.GetValue(o);
                var table = _sqlMapping.GetSqlEquivalent(o.GetType());

                var fromAlias = new AliasDefinition(table);
                var request = new Request()
                {
                    FromAlias = fromAlias,
                    RequestType = RequestType.Delete
                };
                request.Where = new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal, _sqlMapping.GetSqlEquivalent(request, fromAlias, idProperty, false), new Constant(id));

                BeforeDelete?.Invoke(this, new UnitOfWorkEventArgs(connection, id, table, o));

                var parameters = new List<Tuple<string, object>>();
                var sql = _sqlProvider.GetSQLTranslator().Translate(request, parameters);

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

            });
        }

        public void Delete<TSource>(IQueryable<TSource> source)
        {
            _operations.Add(connection => source.Provider.Execute<int>(Expression.Call(Expression.Constant(this), new Action<IQueryable<TSource>>(Delete).Method, source.Expression)));
        }

        public void Update<TSource>(IQueryable<TSource> source, Expression<Func<TSource, TSource>> updateExpression)
        {
            _operations.Add(connection => source.Provider.Execute<int>(Expression.Call(Expression.Constant(this), new Action<IQueryable<TSource>, Expression<Func<TSource, TSource>>>(Update).Method, source.Expression, Expression.Quote(updateExpression))));
        }

        public void Insert<TSource, TInsert>(IQueryable<TSource> source, Expression<Func<TSource, TInsert>> insertExpression)
        {
            _operations.Add(connection => source.Provider.Execute<int>(Expression.Call(Expression.Constant(this), new Action<IQueryable<TSource>, Expression<Func<TSource, TInsert>>>(Insert).Method, source.Expression, Expression.Quote(insertExpression))));
        }

        public void Execute(string sql, IEnumerable<Tuple<string, object>> parameters)
        {
            _operations.Add(connection =>
            {
                using (var cmd = connection.CreateCommand())
                {
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
            using (var connection = _sqlProvider.GetConnection())
            {
                if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                using (var transaction = connection.BeginTransaction())
                {
                    _operations.ForEach(op => op(connection));
                    transaction.Commit();
                }
            }

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
            public event EventHandler<UnitOfWorkEventArgs> BeforeUpdate;

            public event EventHandler<UnitOfWorkEventArgs> BeforeInsert;

            public abstract void Save(IDbConnection connection, object o, PropertyInfo parentProperty, object parentId, HashSet<object> alreadySaved, ISQLProvider sqlProvider, ISQLMapping sqlMapping, Dictionary<object, State> extractedStates);

            protected void OnBeforeUpdate(UnitOfWorkEventArgs args)
            {
                BeforeUpdate?.Invoke(this, args);
            }

            protected void OnBeforeInsert(UnitOfWorkEventArgs args)
            {
                BeforeInsert?.Invoke(this, args);
            }
        }

        private class DoSave<T> : DoSave
        {
            public override void Save(IDbConnection connection, object o, PropertyInfo parentProperty, object parentId, HashSet<object> alreadySaved, ISQLProvider sqlProvider, ISQLMapping sqlMapping, Dictionary<object, State> extractedStates)
            {
                // no infinite loop
                if (alreadySaved.Contains(o)) return;
                alreadySaved.Add(o);

                var idProperty = sqlMapping.GetIdProperty(o.GetType());
                var pacId = new PropertyAccessorCompiler(idProperty);
                var id = pacId.GetPropertyValue(o);

                var doInsert = (idProperty.PropertyType.IsValueType && Activator.CreateInstance(idProperty.PropertyType).Equals(id)) || (!idProperty.PropertyType.IsValueType && id == null);
                var initialState = !doInsert && extractedStates.ContainsKey(o) ? extractedStates[o] : null;
                var newState = initialState != null ? StateExtractor.GetStateExtractor(sqlMapping, typeof(T)).GetAllComponentStates(o)[o] : null;

                var table = sqlMapping.GetSqlEquivalent(o.GetType());
                var fromAlias = new AliasDefinition(table);
                var request = new Request()
                {
                    FromAlias = fromAlias,
                    RequestType = doInsert ? RequestType.InsertValues : RequestType.Update,
                    IdColumnName = sqlMapping.GetColumnName(idProperty)
                };

                PropertyAccessorCompiler[] properties = null;
                if (initialState != null)
                {
                    properties = initialState.GetChangedProperties(newState)
                        .Where(p => p.GetCustomAttribute<TransientAttribute>() == null && p.GetCustomAttribute<InversePropertyAttribute>() == null && p.Name != idProperty.Name)
                        .Select(p => new PropertyAccessorCompiler(p)).ToArray();

                    extractedStates[o] = newState;
                }
                else
                    properties = typeof(T).GetProperties()
                        .Where(p => p.GetCustomAttribute<TransientAttribute>() == null && p.GetCustomAttribute<InversePropertyAttribute>() == null && p.Name != idProperty.Name)
                        .Select(p => new PropertyAccessorCompiler(p)).ToArray();

                if (properties.Length == 0) return;

                foreach (var pac in properties)
                {
                    var value = pac.GetPropertyValue(o);

                    if (pac.Property.PropertyType.IsClass && pac.Property.PropertyType != typeof(string) && pac.Property.PropertyType.GetCustomAttribute<DbValueTypeAttribute>() == null)
                    {
                        if (typeof(System.Collections.IEnumerable).IsAssignableFrom(pac.Property.PropertyType)) continue;

                        if (value != null)
                        {
                            var ds = (DoSave)Activator.CreateInstance(typeof(DoSave<>).MakeGenericType(pac.Property.PropertyType));
                            ds.Save(connection, value, null, null, alreadySaved, sqlProvider, sqlMapping, extractedStates);
                            value = new PropertyAccessorCompiler(sqlMapping.GetIdProperty(value.GetType())).GetPropertyValue(value);
                        }
                    }

                    request.UpdateOrInsert.Add(new UpdateOrInsertInfo(new Constant(value), new ColumnAccess(fromAlias, new Identifier(sqlMapping.GetColumnName(pac.Property)))));
                }

                if (!doInsert) request.Where = new DML.BinaryExpression(BinaryExpressionTypeEnum.Equal, sqlMapping.GetSqlEquivalent(request, fromAlias, idProperty, false), new Constant(id));

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

                if (doInsert)
                    OnBeforeInsert(new UnitOfWorkEventArgs(connection, null, table, o));
                else
                    OnBeforeUpdate(new UnitOfWorkEventArgs(connection, id, table, o));

                var parameters = new List<Tuple<string, object>>();
                var sql = sqlProvider.GetSQLTranslator().Translate(request, parameters);

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
                        pacId.SetPropertyValue(o, Convert.ChangeType(id = cmd.ExecuteScalar(), idProperty.PropertyType));
                    else
                        cmd.ExecuteNonQuery();
                }

                foreach (var property in properties)
                    if (property.Property.PropertyType.IsClass && property.Property.PropertyType != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.Property.PropertyType))
                    {
                        var value = property.GetPropertyValue(o);
                        if (value == null) continue;

                        var doSave = (DoSave)Activator.CreateInstance(typeof(DoSave<>).MakeGenericType(property.Property.PropertyType.GetGenericArguments()[0]));
                        foreach (var obj in (System.Collections.IEnumerable)value)
                        {
                            doSave.Save(connection, obj, property.Property, id, alreadySaved, sqlProvider, sqlMapping, extractedStates);
                        }
                    }
            }
        }
    }

    public class UnitOfWorkEventArgs : EventArgs
    {
        public UnitOfWorkEventArgs(IDbConnection connection, object id, DMLNode table, object entity)
        {
            Connection = connection;
            Id = id;
            Table = table;
            Entity = entity;
        }

        public IDbConnection Connection { get; private set; }

        public object Id { get; private set; }

        public DMLNode Table { get; private set; }

        public object Entity { get; private set; }
    }
}
