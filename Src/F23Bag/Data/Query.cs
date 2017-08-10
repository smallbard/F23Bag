using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;

namespace F23Bag.Data
{
    public class Query<T> : IQueryable<T>, IQueryable, IEnumerable<T>, IEnumerable, IOrderedQueryable<T>, IOrderedQueryable
    {
        private readonly QueryProvider _provider;
        private readonly Expression _expression;

        public Query(ISQLProvider sqlProvider, ISQLMapping sqlMapping, IEnumerable<IExpresstionToSqlAst> customConverters, Func<Type, object> resolve)
            : this(new DbQueryProvider(sqlProvider, sqlMapping, customConverters, resolve), Expression.Parameter(typeof(Query<T>)))
        { }

        internal Query(QueryProvider provider, Expression expression)
        {
            if (provider == null) throw new ArgumentNullException(nameof(provider));
            if (expression == null) throw new ArgumentNullException(nameof(expression));
            if (!typeof(IQueryable<T>).IsAssignableFrom(expression.Type)) throw new ArgumentOutOfRangeException(nameof(expression));

            _provider = provider;
            _expression = expression;
        }

        public event EventHandler<T> ObjectLoaded;

        Expression IQueryable.Expression
        {
            get { return _expression; }
        }

        Type IQueryable.ElementType
        {
            get { return typeof(T); }
        }

        IQueryProvider IQueryable.Provider
        {
            get { return _provider; }
        }

        public IEnumerator<T> GetEnumerator()
        {
            var enumerator = ((IEnumerable<T>)_provider.Execute(_expression)).GetEnumerator();
            if (enumerator is ObjectReader<T>.Enumerator) ((ObjectReader<T>.Enumerator)enumerator).ObjectLoaded += (s, e) => OnObjectLoaded(e);
            return enumerator;
        }

        IEnumerator IEnumerable.GetEnumerator()
        {
            return GetEnumerator();
        }

        public override string ToString()
        {
            return _provider.GetQueryText(_expression);
        }

        protected virtual void OnObjectLoaded(T loadedObject)
        {
            ObjectLoaded?.Invoke(this, loadedObject);
        }
    }
}
