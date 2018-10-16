using F23Bag.Data.DML;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    internal class PropertyAccess
    {
        internal PropertyAccess(AliasDefinition ownerAlias, PropertyInfo property)
        {
            OwnerAlias = ownerAlias;
            Property = property;
        }

        public AliasDefinition OwnerAlias { get; private set; }

        public PropertyInfo Property { get; private set; }

        public override int GetHashCode()
        {
            var hash = 17;

            hash = hash * 23 + OwnerAlias.GetHashCode();
            hash = hash * 23 + Property.DeclaringType.FullName.GetHashCode();
            hash = hash * 23 + Property.Name.GetHashCode();

            return hash;
        }

        public override bool Equals(object obj)
        {
            var propertyAccess = obj as PropertyAccess;
            return propertyAccess != null && propertyAccess.GetHashCode() == GetHashCode();
        }
    }
}
