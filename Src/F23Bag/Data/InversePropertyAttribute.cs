using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    [AttributeUsage(AttributeTargets.Property)]
    public class InversePropertyAttribute : Attribute

    {
        public InversePropertyAttribute(Type inversePropertyOwner, string inversePropertyName)
        {
            InverseProperty = inversePropertyOwner.GetProperty(inversePropertyName);
        }

        public PropertyInfo InverseProperty { get; private set; }
    }
}
