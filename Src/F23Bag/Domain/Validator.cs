using F23Bag.AutomaticUI;
using System.Collections.Generic;
using System.Reflection;
using System.Text.RegularExpressions;

namespace F23Bag.Domain
{
    public class Validator<TDomain>
    {
        private const string cstMandatoryPropertyNotSetMessage = "MandatoryPropertyNotSet";
        private const string cstMaximumLengthExceededMessage = "MaximumLengthExceeded";
        private const string cstRegexNotMatchMessage = "RegexNotMatch";

        private readonly static Dictionary<PropertyInfo, int> _propertiesWithMaxLength = new Dictionary<PropertyInfo, int>();
        private readonly static HashSet<PropertyInfo> _mandatoryProperties = new HashSet<PropertyInfo>();
        private readonly static Dictionary<PropertyInfo, Regex> _propertiesWithRegex = new Dictionary<PropertyInfo, Regex>();
        private readonly I18n _i18n;

        static Validator()
        {
            foreach (var property in typeof(TDomain).GetProperties())
            {
                if (property.GetCustomAttribute<MandatoryAttribute>() != null) _mandatoryProperties.Add(property);

                var maxLengthAtt = property.GetCustomAttribute<MaxLengthAttribute>();
                if (maxLengthAtt != null && property.PropertyType == typeof(string)) _propertiesWithMaxLength[property] = maxLengthAtt.MaxLength;

                var regexAtt = property.GetCustomAttribute<RegexAttribute>();
                if (regexAtt != null && property.PropertyType == typeof(string)) _propertiesWithRegex[property] = regexAtt.Regex;
            }
        }

        public Validator(I18n i18n)
        {
            _i18n = i18n;
        }

        public IEnumerable<ValidationEventArgs> Validate(TDomain objectToValidate)
        {
            foreach (var mandatoryProperty in _mandatoryProperties)
            {
                var value = mandatoryProperty.GetValue(objectToValidate);
                if (value == null)
                    yield return new Domain.ValidationEventArgs(ValidationLevel.Error, mandatoryProperty, _i18n.GetTranslation(cstMandatoryPropertyNotSetMessage));
                else if (value is string && ((string)value).Length == 0)
                    yield return new Domain.ValidationEventArgs(ValidationLevel.Error, mandatoryProperty, _i18n.GetTranslation(cstMandatoryPropertyNotSetMessage));
            }

            foreach (var propertyWithMaxLength in _propertiesWithMaxLength)
            {
                var value = propertyWithMaxLength.Key.GetValue(objectToValidate);
                if (value is string && ((string)value).Length > propertyWithMaxLength.Value) yield return new Domain.ValidationEventArgs(ValidationLevel.Error, propertyWithMaxLength.Key, cstMaximumLengthExceededMessage);
            }

            foreach (var propertyWithRegex in _propertiesWithRegex)
            {
                var value = propertyWithRegex.Key.GetValue(objectToValidate);
                if (value is string && !propertyWithRegex.Value.IsMatch((string)value)) yield return new ValidationEventArgs(ValidationLevel.Error, propertyWithRegex.Key, cstRegexNotMatchMessage);
            }
        }
    }
}
