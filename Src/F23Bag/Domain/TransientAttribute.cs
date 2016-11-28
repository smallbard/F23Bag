using System;

namespace F23Bag.Domain
{
    [AttributeUsage(AttributeTargets.Property)]
    public class TransientAttribute : Attribute
    {
    }
}
