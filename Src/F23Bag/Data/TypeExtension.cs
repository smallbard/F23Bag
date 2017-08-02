using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    internal static class TypeExtension
    {
        public static bool IsSimpleMappedType(this Type t)
        {
            return (!t.IsClass && !t.IsInterface) || t == typeof(string) || t.GetCustomAttribute<DbValueTypeAttribute>() != null;
        }

        public static bool IsEntityOrCollection(this Type t)
        {
            return t != typeof(string) && (t.IsClass || t.IsInterface) && t.GetCustomAttribute<DbValueTypeAttribute>() == null;
        }

        public static bool IsCollection(this Type t)
        {
            return t != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(t);
        }
    }
}
