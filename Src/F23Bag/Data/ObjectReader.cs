using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using F23Bag.Data.DML;
using F23Bag.Data.Mapping;
using System.Linq.Expressions;

namespace F23Bag.Data
{
    internal class ObjectReader<T> : IEnumerable<T>, IEnumerable
    {
        private Enumerator _enumerator;

        internal ObjectReader(DbConnection connection, DbCommand command, DbDataReader reader, Request request, ISQLTranslator sqlTranslator, Mapper mapper, Func<Type, object> resolver)
        {
            _enumerator = new Enumerator(connection, command, reader, request, sqlTranslator, mapper, resolver);
        }

        public IEnumerator<T> GetEnumerator()
        {
            var e = _enumerator;
            if (e == null) throw new InvalidOperationException("Cannot enumerate more than once.");
            _enumerator = null;
            return e;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        private class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private static readonly MethodInfo _changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
            private readonly DbConnection _connection;
            private readonly DbCommand _command;
            private readonly DbDataReader _reader;
            private readonly Request _request;
            private readonly ISQLTranslator _sqlTranslator;
            private readonly bool _isSimpleType;
            private readonly bool _isAnonymousType;
            private readonly Mapper _mapper;
            private readonly Func<Type, object> _resolver;
            private readonly Func<T> _createNew;
            private readonly Func<DbDataReader, T> _createNewAnonymousType;
            private T _current;
            private object _lastId;
            private bool _notEndOfReader;

            internal Enumerator(DbConnection connection, DbCommand command, DbDataReader reader, Request request, ISQLTranslator sqlTranslator, Mapper mapper, Func<Type, object> resolver)
            {
                _connection = connection;
                _command = command;
                _reader = reader;
                _request = request;
                _sqlTranslator = sqlTranslator;
                _isSimpleType = typeof(T) == typeof(string) || !typeof(T).IsClass;
                _mapper = mapper;
                _resolver = resolver;
                _notEndOfReader = true;

                if (!_isSimpleType)
                {
                    var ci = typeof(T).GetConstructor(new Type[] { });
                    if (ci == null)
                    {
                        _isAnonymousType = true;

                        var pDataReader = Expression.Parameter(typeof(DbDataReader));
                        var variables = new List<ParameterExpression>();
                        var body = new List<Expression>();

                        ci = typeof(T).GetConstructors()[0];
                        if (ci.GetParameters().Any(p => p.ParameterType.IsInterface)) _isAnonymousType = false; // simple type with constructor injection

                        var parameters = ci.GetParameters();
                        for (var i = 0; i < parameters.Length; i++)
                        {
                            Expression exp = null;
                            if (parameters[i].ParameterType.IsClass)
                                exp = Expression.TypeAs(
                                        Expression.MakeIndex(pDataReader, typeof(DbDataReader).GetProperties().First(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(int)), new[] { Expression.Constant(i) }),
                                        parameters[i].ParameterType);
                            else if (parameters[i].ParameterType.IsInterface)
                                exp = Expression.Constant(_resolver(parameters[i].ParameterType));
                            else
                                exp = Expression.Convert(
                                        Expression.Call(_changeTypeMethod,
                                            Expression.MakeIndex(pDataReader, typeof(DbDataReader).GetProperties().First(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(int)), new[] { Expression.Constant(i) }),
                                            Expression.Constant(parameters[i].ParameterType)), parameters[i].ParameterType);

                            var variable = Expression.Variable(parameters[i].ParameterType);
                            body.Add(
                                Expression.Assign(
                                    variable,
                                    exp));
                            variables.Add(variable);
                        }

                        var returnTarget = Expression.Label(typeof(T));
                        body.Add(Expression.Return(returnTarget, Expression.New(ci, variables), typeof(T)));
                        body.Add(Expression.Label(returnTarget, Expression.Constant(default(T), typeof(T))));

                        if (_isAnonymousType)
                            _createNewAnonymousType = Expression.Lambda<Func<DbDataReader, T>>(Expression.Block(variables, body), pDataReader).Compile();
                        else
                            _createNew = Expression.Lambda<Func<T>>(Expression.Block(variables, body)).Compile();
                    }
                    else
                        _createNew = Expression.Lambda<Func<T>>(Expression.New(ci)).Compile();
                }
            }

            public T Current
            {
                get { return _current; }
            }

            object IEnumerator.Current
            {
                get { return _current; }
            }

            public bool MoveNext()
            {
                if (_isSimpleType)
                {
                    if (_reader.Read())
                    {
                        var value = _reader[0];
                        _current = DBNull.Value.Equals(value) ? default(T) : (T)value;
                        return true;
                    }

                    return false;
                }
                else if(_isAnonymousType)
                {
                    if (_reader.Read())
                    {
                        _current = _createNewAnonymousType(_reader);
                        return true;
                    }

                    return false;
                }

                if (_lastId == null)
                {
                    if (!_reader.Read()) return false;
                    _lastId = _mapper.GetMainId(_reader, _request);
                }

                var id = _lastId;
                _current = _createNew();
                var firstRead = true;
                if (_notEndOfReader)
                    do
                    {
                        _mapper.Map(_current, _reader, _request, firstRead);
                        firstRead = false;
                    }
                    while ((_notEndOfReader = _reader.Read()) && (_lastId = _mapper.GetMainId(_reader, _request)).Equals(id));
                else
                    return false;

                return true;
            }

            public void Reset() { }

            public void Dispose()
            {
                _reader.Dispose();
                _command.Dispose();
                _connection.Dispose();
            }
        }
    }
}
