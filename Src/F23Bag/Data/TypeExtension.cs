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
        public static bool IsSimpleMappedType(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return (!type.IsClass && !type.IsInterface) || type == typeof(string) || type.GetDbValueType() != type;
        }

        public static bool IsEntityOrCollection(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type != typeof(string) && !type.IsArray && (type.IsClass || type.IsInterface) && type.GetDbValueType() == type;
        }

        public static bool IsCollection(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type != typeof(string) && typeof(System.Collections.IEnumerable).IsAssignableFrom(type);
        }

        public static Type GetDbValueType(this Type type)
        {
            if (type == null) throw new ArgumentNullException(nameof(type));

            return type.GetInterfaces().FirstOrDefault(i => i.IsGenericType && i.GetGenericTypeDefinition() == typeof(IDbValueType<>))?.GetGenericArguments()[0] ?? type;
        }
    }
}
