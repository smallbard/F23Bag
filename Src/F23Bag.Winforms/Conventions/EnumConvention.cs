using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.Winforms.Controls;

namespace F23Bag.Winforms.Conventions
{
    public class EnumConvention : IControlConvention
    {
        public bool Accept(PropertyInfo property, OneMemberLayout layout)
        {
            return property.PropertyType.IsEnum || (property.PropertyType.IsGenericType && property.PropertyType.GetGenericArguments()[0].IsEnum);
        }

        public DataControl GetControl(PropertyInfo property, OneMemberLayout layout, WinformContext context)
        {
            return new EnumControl(layout, context, property, layout.Label) { Enabled = layout.IsEditable };
        }
    }
}
