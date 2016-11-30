using F23Bag.AutomaticUI.Layouts;
using System;

namespace F23Bag.AutomaticUI
{
    /// <summary>
    /// Build the UI for a given data type.
    /// </summary>
    public interface IUIBuilder
    {
        void Display(Layout layout, object data, string label);
    }
}
