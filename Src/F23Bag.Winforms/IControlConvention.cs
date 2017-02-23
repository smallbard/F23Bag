using System.Reflection;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.Winforms.Controls;
using System;

namespace F23Bag.Winforms
{
    public interface IControlConvention
    {
        bool Accept(PropertyInfo property, OneMemberLayout layout);

        DataControl GetControl(object data, PropertyInfo property, OneMemberLayout layout, WinformContext context);
    }
}