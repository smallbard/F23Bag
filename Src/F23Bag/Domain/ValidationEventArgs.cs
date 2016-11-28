using System;
using System.Reflection;

namespace F23Bag.Domain
{
    public class ValidationEventArgs : EventArgs
    {
        public ValidationEventArgs(ValidationLevel level, PropertyInfo property, string message)
        {
            Level = level;
            Property = property;
            Message = message;
        }

        public ValidationLevel Level { get; private set; }

        public PropertyInfo Property { get; private set; }

        public string Message { get; private set; }
    }

    public enum ValidationLevel
    {
        None,
        Information,
        Warning,
        Error
    }
}