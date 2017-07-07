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
        private bool _isRegrouped;

        private LoadingPropertyInfo(PropertyInfo property, LazyLoadingType lazyLoadingType)
        {
            if ((!property.PropertyType.IsClass && !property.PropertyType.IsInterface) || property.PropertyType == typeof(string) || property.PropertyType.GetCustomAttribute<DbValueTypeAttribute>() != null)
                throw new NotSupportedException("LazyLoad and EagerLoad are not available for simple types : " + property.Name);
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
            var sb = new System.Text.StringBuilder(Property.Name);
            if (SubLoadingPropertyInfo.Count > 0) sb.Append('.').Append(SubLoadingPropertyInfo[0].ToString());
            return _asString = sb.ToString();
        }

        public IEnumerable<LoadingPropertyInfo> GetSubLoadingPropertyInfoforLazy()
        {
            var lst = SubLoadingPropertyInfo.Select(lpi =>
            {
                lpi.Parent = null;
                lpi._isRegrouped = true;
                return lpi;
            }).ToList();

            return lst;
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
            var initialDepths = infos.ToDictionary(i => i, i => i.Depth);

            foreach (var info in infos.Where(i => i.Depth > 0 && !i._isRegrouped).OrderByDescending(i => i.Depth).ToList())
            {
                var depth = initialDepths[info];
                var realParent = infos.FirstOrDefault(i => initialDepths[i] == depth - 1 && info.ToString().StartsWith(i.ToString()));
                if (realParent == null) throw new NotSupportedException("LazyLoad and EagerLoad must be called on each intermediate property : " + info.ToString());

                realParent = realParent.LastSubLoadingPropertyInfo;

                infos.Remove(info);

                var sub = GetChildAtDepth(info, depth);
                realParent = SearchImmediateParentDescendant(realParent, sub);
                realParent.SubLoadingPropertyInfo.Add(sub);
                sub.Parent = realParent;
            }
        }

        private LoadingPropertyInfo LastSubLoadingPropertyInfo
        {
            get
            {
                if (SubLoadingPropertyInfo.Count == 0) return this;
                return SubLoadingPropertyInfo[0].LastSubLoadingPropertyInfo;
            }
        }

        private static LoadingPropertyInfo GetChildAtDepth(LoadingPropertyInfo root, int depth)
        {
            if (depth == 0) return root;
            if (depth == 1) return root.SubLoadingPropertyInfo.First();
            return GetChildAtDepth(root.SubLoadingPropertyInfo.First(), depth - 1);
        }

        private static LoadingPropertyInfo SearchImmediateParentDescendant(LoadingPropertyInfo root, LoadingPropertyInfo child)
        {
            if (root.Property.DeclaringType != child.Parent.Property.DeclaringType && root.Property.Name != child.Parent.Property.Name)
            {
                if (root.SubLoadingPropertyInfo.Count == 0) return SearchImmediateParentAscendant(root, child);

                foreach (var rootChild in root.SubLoadingPropertyInfo)
                {
                    var immediateParent = SearchImmediateParentDescendant(rootChild, child);
                    if (immediateParent != null) return immediateParent;
                }

                return null;
            }

            return root;
        }

        private static LoadingPropertyInfo SearchImmediateParentAscendant(LoadingPropertyInfo root, LoadingPropertyInfo child)
        {
            if (root.Property.DeclaringType != child.Parent.Property.DeclaringType && root.Property.Name != child.Parent.Property.Name)
            {
                if (root.Parent == null) return null;

                var immediateParent = SearchImmediateParentAscendant(root.Parent, child);
                if (immediateParent != null) return immediateParent;

                return null;
            }

            return root;
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
                var parentLpi = lpi;
                while (lpi.SubLoadingPropertyInfo.Count > 0) lpi = lpi.SubLoadingPropertyInfo.First();
                lpi.SubLoadingPropertyInfo.Add(new LoadingPropertyInfo(property, lazyLoadingType) { Parent = lpi });
                return parentLpi;
            }
            else if (propertyAccess.Expression is MemberExpression)
            {
                var lpi = FromExpression(expression, lazyLoadingType, (MemberExpression)propertyAccess.Expression);
                var parentLpi = lpi;
                while (lpi.SubLoadingPropertyInfo.Count > 0) lpi = lpi.SubLoadingPropertyInfo.First();
                lpi.SubLoadingPropertyInfo.Add(new LoadingPropertyInfo(property, lazyLoadingType) { Parent = lpi });
                return parentLpi;
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
