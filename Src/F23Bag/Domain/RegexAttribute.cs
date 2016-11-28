using System;
using System.Text.RegularExpressions;

namespace F23Bag.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    public class RegexAttribute : Attribute
    {
        public RegexAttribute(string pattern)
        {
            Regex = new Regex(pattern, RegexOptions.Compiled);
        }

        public Regex Regex { get; private set; }
    }
}
