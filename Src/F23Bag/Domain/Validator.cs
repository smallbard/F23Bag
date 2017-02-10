using F23Bag.AutomaticUI;
using F23Bag.Data;
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace F23Bag.Domain
{
    public class Validator<TDomain>
    {
        private const string cstMandatoryPropertyNotSetCodeMessage = "Validation_MandatoryPropertyNotSet";
        private const string cstMaximumLengthExceededCodeMessage = "Validation_MaximumLengthExceeded";
        private const string cstRegexNotMatchCodeMessage = "Validation_RegexNotMatch";

        private readonly static Dictionary<PropertyInfo, int> _propertiesWithMaxLength = new Dictionary<PropertyInfo, int>();
        private readonly static HashSet<Tuple<PropertyInfo, Func<object, object>>> _mandatoryProperties = new HashSet<Tuple<PropertyInfo, Func<object, object>>>();
        private readonly static Dictionary<Tuple<PropertyInfo, Func<object, object>>, Regex> _propertiesWithRegex = new Dictionary<Tuple<PropertyInfo, Func<object, object>>, Regex>();

        static Validator()
        {
            foreach (var property in typeof(TDomain).GetProperties())
            {
                if (property.GetCustomAttribute<MandatoryAttribute>() != null) _mandatoryProperties.Add(Tuple.Create(property, new PropertyAccessorCompiler(property).GetPropertyValue));

                var maxLengthAtt = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLengthAtt != null && property.PropertyType == typeof(string)) _propertiesWithMaxLength[property] = maxLengthAtt.MaxLength;

                var regexAtt = property.GetCustomAttribute<RegexAttribute>();
                if (regexAtt != null && property.PropertyType == typeof(string)) _propertiesWithRegex[Tuple.Create(property, new PropertyAccessorCompiler(property).GetPropertyValue)] = regexAtt.Regex;
            }
        }

        public IEnumerable<ValidationEventArgs> Validate(TDomain objectToValidate)
        {
            foreach (var mandatoryProperty in _mandatoryProperties)
            {
                var value = mandatoryProperty.Item2(objectToValidate);
                if (value == null)
                    yield return new Domain.ValidationEventArgs(ValidationLevel.Error, mandatoryProperty.Item1, cstMandatoryPropertyNotSetCodeMessage, null);
                else if (value is string && ((string)value).Length == 0)
                    yield return new Domain.ValidationEventArgs(ValidationLevel.Error, mandatoryProperty.Item1, cstMandatoryPropertyNotSetCodeMessage, null);
            }

            foreach (var propertyWithMaxLength in _propertiesWithMaxLength)
            {
                var value = propertyWithMaxLength.Key.GetValue(objectToValidate);
                if (value is string && ((string)value).Length > propertyWithMaxLength.Value) yield return new Domain.ValidationEventArgs(ValidationLevel.Error, propertyWithMaxLength.Key, cstMaximumLengthExceededCodeMessage, null);
            }

            foreach (var propertyWithRegex in _propertiesWithRegex)
            {
                var value = propertyWithRegex.Key.Item2(objectToValidate);
                if (value is string && !propertyWithRegex.Value.IsMatch((string)value)) yield return new ValidationEventArgs(ValidationLevel.Error, propertyWithRegex.Key.Item1, cstRegexNotMatchCodeMessage, null);
            }
        }
    }
}
