using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data.Mapping
{
    internal class LoadingPropertyInfo
    {
        private string _asString;

        private LoadingPropertyInfo(PropertyInfo property, LazyLoadingType lazyLoadingType)
        {
            if ((!property.PropertyType.IsClass && !property.PropertyType.IsInterface) || property.PropertyType == typeof(string)) throw new NotSupportedException("LazyLoad and EagerLoad are not available for simple types : " + property.Name);
            if (property.GetGetMethod() == null || property.GetSetMethod() == null) throw new NotSupportedException("LazyLoad and EagerLoad need a property with public get and public set : " + property.Name);

            Property = property;
            LazyLoadingType = lazyLoadingType;
            SubLoadingPropertyInfo = new List<Mapping.LoadingPropertyInfo>();
            var pac = new PropertyAccessorCompiler(property);
            SetPropertyValue = pac.SetPropertyValue;
            GetPropertyValue = pac.GetPropertyValue;
        }

        public PropertyInfo Property { get; private set; }

        public Action<object, object> SetPropertyValue { get; private set; }

        public Func<object,object> GetPropertyValue { get; private set; }

        public LazyLoadingType LazyLoadingType { get; private set; }

        public List<LoadingPropertyInfo> SubLoadingPropertyInfo { get; private set; }

        public LoadingPropertyInfo Parent { get; private set; }

        public int Depth
        {
            get
            {
                if (SubLoadingPropertyInfo.Count == 0) return 0;
                return SubLoadingPropertyInfo.Max(slpi => slpi.Depth + 1);
            }
        }

        public override string ToString()
        {
            if (_asString != null) return _asString;

            if (Parent == null) return _asString = Property.Name;
            return _asString = Parent.ToString() + "." + Property.Name;
        }

        private LoadingPropertyInfo LastSubLoadingPropertyInfo
        {
            get
            {
                if (SubLoadingPropertyInfo.Count == 0) return this;
                return SubLoadingPropertyInfo[0].LastSubLoadingPropertyInfo;
            }
        }

        public static LoadingPropertyInfo FromExpression(MethodCallExpression expression)
        {
            var propertyAccess = ((LambdaExpression)ExpressionToSqlAst.StripQuotes(expression.Arguments[1])).Body as MemberExpression;
            if (propertyAccess == null) throw new NotSupportedException("LazyLoad and EagerLoad are available for property only : " + expression.ToString());

            var lazyLoadingType = expression.Method.Name == "LazyLoad" ? LazyLoadingType.Simple : (expression.Method.Name == "BatchLazyLoad" ? LazyLoadingType.Batch : LazyLoadingType.None);

            return FromExpression(expression, lazyLoadingType, propertyAccess);
        }

        public static void RegroupLoadingInfo(List<LoadingPropertyInfo> infos)
        {
            foreach (var info in infos.Where(i => i.Depth > 0).OrderByDescending(i => i.Depth).ToList())
            {
                var depth = info.Depth;
                var realParent = infos.FirstOrDefault(i => i.Depth == depth - 1 && info.ToString().StartsWith(i.ToString()));
                if (realParent == null) throw new NotSupportedException("LazyLoad and EagerLoad must be called on each intermediate property : " + info.ToString());

                infos.Remove(info);
                var sub = info.LastSubLoadingPropertyInfo;
                sub.Parent = realParent;
                realParent.SubLoadingPropertyInfo.Add(sub);
            }
        }

        private static LoadingPropertyInfo FromExpression(MethodCallExpression expression, LazyLoadingType lazyLoadingType, MemberExpression propertyAccess)
        {
            var property = propertyAccess.Member as PropertyInfo;
            if (property == null) throw new NotSupportedException("LazyLoad and EagerLoad are available for property only : " + expression.ToString());

            if (propertyAccess.Expression is ParameterExpression)
                return new LoadingPropertyInfo(property, lazyLoadingType);
            else if (propertyAccess.Expression is MethodCallExpression && ((MethodCallExpression)propertyAccess.Expression).Method.Name == "First" && ((MethodCallExpression)propertyAccess.Expression).Arguments[0] is MemberExpression)
            {
                var lpi = FromExpression(expression, lazyLoadingType, (MemberExpression)((MethodCallExpression)propertyAccess.Expression).Arguments[0]);
                lpi.SubLoadingPropertyInfo.Add(new LoadingPropertyInfo(property, lazyLoadingType) { Parent = lpi });
                return lpi;
            }
            else
                throw new NotSupportedException("LazyLoad and EagerLoad support only property access and First method call : " + expression.ToString());
        }
    }

    internal enum LazyLoadingType
    {
        None,
        Simple,
        Batch
    }
}
