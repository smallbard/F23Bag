using System;
using System.Linq;
using System.Linq.Expressions;

namespace F23Bag.Data
{
    public static class QueryableExtension
    {
        public static IQueryable<TSource> EagerLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));

            if (!(source.Provider is QueryProvider))
            {
                return source;
            }

            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(EagerLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> LazyLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));

            if (!(source.Provider is QueryProvider))
            {
                return source;
            }

            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(LazyLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> BatchLazyLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));

            if (!(source.Provider is QueryProvider))
            {
                return source;
            }

            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(BatchLazyLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> DontLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));

            if (!(source.Provider is QueryProvider))
            {
                return source;
            }

            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(DontLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> AddParameter<TSource>(this IQueryable<TSource> source, string parameterName, object value)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (string.IsNullOrEmpty(parameterName)) throw new ArgumentNullException(nameof(parameterName));

            (source.Provider as DbQueryProvider)?.AddParameter(parameterName, value == null ? DBNull.Value : value);

            return source;
        }
    }
}
