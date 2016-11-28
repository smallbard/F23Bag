using System.Reflection;
using F23Bag.AutomaticUI.Layouts;
using F23Bag.Winforms.Controls;

namespace F23Bag.Winforms
{
    public interface IControlConvention
    {
        bool Accept(PropertyInfo property, OneMemberLayout layout);

        DataControl GetControl(PropertyInfo property, OneMemberLayout layout);
    }
}