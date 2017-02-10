using F23Bag.AutomaticUI;
using System;
using System.Collections.Generic;
using System.Reflection;

namespace F23Bag.Domain
{
    public class ValidationEventArgs : EventArgs, I18nMessage
    {
        public ValidationEventArgs(ValidationLevel level, PropertyInfo property, string codeMessage, object parameters)
        {
            Level = level;
            Property = property;
            CodeMessage = codeMessage;
            Parameters = parameters ?? new object[] { };
        }

        public ValidationLevel Level { get; private set; }

        public PropertyInfo Property { get; private set; }

        public string CodeMessage { get; private set; }

        public object Parameters { get; private set; }
    }

    public enum ValidationLevel
    {
        None,
        Information,
        Warning,
        Error
    }
}