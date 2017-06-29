using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Reflection;

namespace F23Bag.Data
{
    internal class PropertyAccessorCompiler
    {
        private static Dictionary<PropertyInfo, Action<object, object>> _setValues = new Dictionary<PropertyInfo, Action<object, object>>();
        private static Dictionary<PropertyInfo, Func<object, object>> _getValues = new Dictionary<PropertyInfo, Func<object, object>>();

        public PropertyAccessorCompiler(PropertyInfo property)
        {
            if (property == null) return;

            Property = property;

            if (property.CanWrite)
            {
                if (!_setValues.ContainsKey(property))
                {
                    var paramObj = Expression.Parameter(typeof(object));
                    var paramValue = Expression.Parameter(typeof(object));
                    _setValues[property] = Expression.Lambda<Action<object, object>>(
                        Expression.Assign(Expression.MakeMemberAccess(Expression.Convert(paramObj, property.DeclaringType), property), Expression.Convert(paramValue, property.PropertyType)), paramObj, paramValue).Compile();
                }

                SetPropertyValue = _setValues[property];
            }

            if (property.CanRead)
            {
                if (!_getValues.ContainsKey(property))
                {
                    var paramObj = Expression.Parameter(typeof(object));
                    _getValues[property] = Expression.Lambda<Func<object, object>>(Expression.Convert(Expression.MakeMemberAccess(Expression.Convert(paramObj, property.DeclaringType), property), typeof(object)), paramObj).Compile();
                }

                GetPropertyValue = _getValues[property];
            }
        }

        public PropertyInfo Property { get; private set; }

        public Action<object, object> SetPropertyValue { get; private set; }

        public Func<object,object> GetPropertyValue { get; private set; }
    }
}
