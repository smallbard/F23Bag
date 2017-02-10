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

            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(EagerLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> LazyLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(LazyLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> BatchLazyLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(BatchLazyLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> DontLoad<TSource, TValue>(this IQueryable<TSource> source, Expression<Func<TSource, TValue>> propertyExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (propertyExpression == null) throw new ArgumentNullException(nameof(propertyExpression));
            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TValue>>, IQueryable<TSource>>(DontLoad).Method, source.Expression, Expression.Quote(propertyExpression)));
        }

        public static IQueryable<TSource> Recursive<TSource>(this IQueryable<TSource> source, Expression<Func<TSource, TSource, bool>> recursiveExpression)
        {
            if (source == null) throw new ArgumentNullException(nameof(source));
            if (recursiveExpression == null) throw new ArgumentNullException(nameof(recursiveExpression));
            return new Query<TSource>((QueryProvider)source.Provider, Expression.Call(null, new Func<IQueryable<TSource>, Expression<Func<TSource, TSource, bool>>, IQueryable<TSource>>(Recursive).Method, source.Expression, Expression.Quote(recursiveExpression)));
        }
    }
}
