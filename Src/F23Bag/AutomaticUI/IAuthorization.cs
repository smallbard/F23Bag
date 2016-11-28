using System;
using System.Reflection;

namespace F23Bag.AutomaticUI
{
    /// <summary>
    /// Interface for authorization management.
    /// </summary>
    public interface IAuthorization
    {
        /// <summary>
        /// Tell if this instance manage the given data type.
        /// </summary>
        /// <param name="type">Type for which we want to manage authorizations.</param>
        /// <returns>True if this instance manage the given data type.</returns>
        bool Accept(Type type);

        bool IsVisible(object data, MemberInfo member);

        bool IsEditable(object data, PropertyInfo property);

        bool IsEnable(object data, MethodInfo method);
    }    
}
