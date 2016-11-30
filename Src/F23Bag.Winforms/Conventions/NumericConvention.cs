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
    public class NumericConvention : IControlConvention
    {
        private static readonly Type[] _numericTypes = new[] { typeof(int), typeof(decimal), typeof(double), typeof(short), typeof(long), typeof(byte), typeof(uint), typeof(ushort), typeof(ulong) };

        public bool Accept(PropertyInfo property, OneMemberLayout layout)
        {
            return _numericTypes.Contains(property.PropertyType) && layout.ItemsSource == null;
        }

        public DataControl GetControl(PropertyInfo property, OneMemberLayout layout, WinformContext context)
        {
            return new NumericControl(layout, context, property, layout.Label) { Enabled = layout.IsEditable };
        }
    }
}
