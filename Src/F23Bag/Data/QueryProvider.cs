using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data
{
    public abstract class QueryProvider : IQueryProvider
    {
        protected QueryProvider() { }

        IQueryable<TSource> IQueryProvider.CreateQuery<TSource>(Expression expression)
        {
            return new Query<TSource>(this, expression);
        }

        IQueryable IQueryProvider.CreateQuery(Expression expression)
        {
            var elementType = typeof(System.Collections.IEnumerable).IsAssignableFrom(expression.Type) ? expression.Type.GetGenericArguments()[0] : expression.Type;
            try
            {
                return (IQueryable)Activator.CreateInstance(typeof(Query<>).MakeGenericType(elementType), new object[] { this, expression });
            }
            catch (TargetInvocationException tie)
            {
                throw tie.InnerException;
            }
        }

        TResult IQueryProvider.Execute<TResult>(Expression expression)
        {
            var value = Execute(expression);
            try
            {
                return (TResult)(value ?? default(TResult));
            }
            catch (InvalidCastException)
            {
                return (TResult)Convert.ChangeType(value, typeof(TResult));
            }
        }

        object IQueryProvider.Execute(Expression expression)
        {
            return Execute(expression);
        }

        public abstract string GetQueryText(Expression expression);

        public abstract object Execute(Expression expression);
    }
}
