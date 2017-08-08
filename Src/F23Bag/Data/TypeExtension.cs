using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    public static class TypeExtension
    {
        public static bool IsSimpleMappedType(this Type t)
        {
            return (!t.IsClass && !t.IsInterface) || t == typeof(string) || t.GetDbValueType() != t;
        }

        public static bool IsEntityOrCollection(this Type t)
        {
            return t != typeof(string) && !t.IsArray && (t.IsClass || t.IsInterface) && t.GetDbValueType() == t;
        }

        public static bool IsCollection(this Type t)
        {
            return t != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(t);
        }

        public static Type GetDbValueType(this Type t)
        {
            return t.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbValueType<>))?.GetGenericArguments()[0] ?? t;
        }
    }
}
