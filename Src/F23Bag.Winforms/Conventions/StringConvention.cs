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
    public class StringConvention : IControlConvention
    {
        public bool Accept(PropertyInfo property, OneMemberLayout layout)
        {
            return property.PropertyType == typeof(string) && layout.ItemsSource == null;
        }

        public DataControl GetControl(PropertyInfo property, OneMemberLayout layout, WinformContext context)
        {
            return new StringControl(layout, context, property, layout.Label) { Enabled = layout.IsEditable };
        }
    }
}
