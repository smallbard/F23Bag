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
    public class DataGridConvention : IControlConvention
    {
        public bool Accept(PropertyInfo property, OneMemberLayout layout)
        {
            return typeof(string) != property.PropertyType && typeof(System.Collections.IEnumerable).IsAssignableFrom(property.PropertyType);
        }

        public DataControl GetControl(object data, PropertyInfo property, OneMemberLayout layout, WinformContext context)
        {
            var dataGridLayout = layout.GetDataGridLayout(property.PropertyType.GetGenericArguments()[0]);
            if (dataGridLayout == null) throw new WinformsException("No datagrid layout for " + property.PropertyType.GetGenericArguments()[0].FullName);

            var dataGridControl = new DataGridControl(layout, context, dataGridLayout.OpenAction);

            foreach (var subLayout in dataGridLayout.Columns)
                if (subLayout.Member is PropertyInfo)
                    dataGridControl.AddColumn((PropertyInfo)subLayout.Member, subLayout.IsEditable, subLayout.Label);
                else if (subLayout.Member is MethodInfo)
                    dataGridControl.AddColumn((MethodInfo)subLayout.Member, subLayout.Label);
            foreach (var subLayout in dataGridLayout.Actions.Where(l => l.Member is MethodInfo)) dataGridControl.AddAction(subLayout, (MethodInfo)subLayout.Member, subLayout.Label);

            dataGridControl.Property = property;
            dataGridControl.Label = layout.Label;

            return dataGridControl;
        }
    }
}
