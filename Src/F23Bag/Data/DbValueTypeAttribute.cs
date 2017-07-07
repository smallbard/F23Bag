using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    [AttributeUsage(AttributeTargets.Class, Inherited = true)]
    public class DbValueTypeAttribute : Attribute
    {
        public DbValueTypeAttribute(Type equivalentType)
        {
            EquivalentType = equivalentType;
        }

        public Type EquivalentType { get; private set; }
    }
}
