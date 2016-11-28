using System;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data
{
    public abstract class QueryProvider : IQueryProvider
    {
        protected QueryProvider() { }

        IQueryable<S> IQueryProvider.CreateQuery<S>(Expression expression)
        {
            return new Query<S>(this, expression);
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

        S IQueryProvider.Execute<S>(Expression expression)
        {
            var value = Execute(expression);
            try
            {
                return (S)(value ?? default(S));
            }
            catch (InvalidCastException)
            {
                return (S)Convert.ChangeType(value, typeof(S));
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
