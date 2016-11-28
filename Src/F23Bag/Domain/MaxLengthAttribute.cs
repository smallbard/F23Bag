using System;

namespace F23Bag.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    public class MaxLengthAttribute : Attribute
    {
        public MaxLengthAttribute(int maxLength)
        {
            if (maxLength <= 0) throw new ArgumentException("maxLength must be upper than 0.", nameof(maxLength));
            MaxLength = maxLength;
        }

        public int MaxLength { get; private set; }
    }
}
