using System;
using System.Collections;
using System.Collections.Generic;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using F23Bag.Data.DML;
using F23Bag.Data.Mapping;
using System.Linq.Expressions;
using System.Data;

namespace F23Bag.Data
{
    internal class ObjectReader<T> : IEnumerable<T>, IEnumerable
    {
        private Enumerator _enumerator;

        internal ObjectReader(IDbConnection connection, IDbCommand command, IDataReader reader, Request request, ISQLTranslator sqlTranslator, Mapper mapper, Func<Type, object> resolver)
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

        public class Enumerator : IEnumerator<T>, IEnumerator, IDisposable
        {
            private static readonly MethodInfo _changeTypeMethod = typeof(Convert).GetMethod("ChangeType", new Type[] { typeof(object), typeof(Type) });
            private static readonly PropertyInfo _indexerDataReader = typeof(IDataRecord).GetProperties().First(p => p.GetIndexParameters().Length == 1 && p.GetIndexParameters()[0].ParameterType == typeof(int));

            private readonly IDbConnection _connection;
            private readonly IDbCommand _command;
            private readonly IDataReader _reader;
            private readonly Request _request;
            private readonly ISQLTranslator _sqlTranslator;
            private readonly bool _isSimpleType;
            private readonly bool _isAnonymousType;
            private readonly Mapper _mapper;
            private readonly Func<Type, object> _resolver;
            private readonly Func<T> _createNew;
            private readonly Func<IDataReader, T> _createNewAnonymousType;
            private T _current;
            private object _lastId;
            private bool _notEndOfReader;

            internal Enumerator(IDbConnection connection, IDbCommand command, IDataReader reader, Request request, ISQLTranslator sqlTranslator, Mapper mapper, Func<Type, object> resolver)
            {
                _connection = connection;
                _command = command;
                _reader = reader;
                _request = request;
                _sqlTranslator = sqlTranslator;
                _isSimpleType = typeof(T).IsSimpleMappedType();
                _mapper = mapper;
                _resolver = resolver;
                _notEndOfReader = true;

                if (!_isSimpleType)
                {
                    var ci = typeof(T).GetConstructor(new Type[] { });
                    if (ci == null)
                    {
                        _isAnonymousType = true;

                        var pDataReader = Expression.Parameter(typeof(IDataReader));
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
                                        Expression.MakeIndex(pDataReader, _indexerDataReader, new[] { Expression.Constant(i) }),
                                        parameters[i].ParameterType);
                            else if (parameters[i].ParameterType.IsInterface)
                                exp = Expression.Constant(_resolver(parameters[i].ParameterType));
                            else
                                exp = Expression.Convert(
                                        Expression.Call(_changeTypeMethod,
                                            Expression.MakeIndex(pDataReader, _indexerDataReader, new[] { Expression.Constant(i) }),
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
                            _createNewAnonymousType = Expression.Lambda<Func<IDataReader, T>>(Expression.Block(variables, body), pDataReader).Compile();
                        else
                            _createNew = Expression.Lambda<Func<T>>(Expression.Block(variables, body)).Compile();
                    }
                    else
                        _createNew = Expression.Lambda<Func<T>>(Expression.New(ci)).Compile();
                }
            }

            public event EventHandler<T> ObjectLoaded;

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

                if (_isAnonymousType)
                {
                    if (_reader.Read())
                    {
                        _current = _createNewAnonymousType(_reader);
                        OnObjectLoaded(_current);
                        return true;
                    }

                    return false;
                }

                if (_lastId == null)
                {
                    if (!_reader.Read()) return false;
                    _lastId = _mapper.GetMainId(_reader, _request);
                    if (_lastId == null) throw new NotSupportedException("There must be an id column.");
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
                    while ((_notEndOfReader = _reader.Read()) && (_lastId = _mapper.GetMainId(_reader, _request)).Equals(id) && id != null);
                else
                    return false;

                OnObjectLoaded(_current);
                return true;
            }

            public void Reset() { }

            public void Dispose()
            {
                _reader.Dispose();
                _command.Dispose();
                _connection.Dispose();
            }

            protected virtual void OnObjectLoaded(T o)
            {
                ObjectLoaded?.Invoke(this, o);
            }
        }
    }
}
