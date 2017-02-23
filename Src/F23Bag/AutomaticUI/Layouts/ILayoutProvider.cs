using System;
using System.Collections.Generic;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Provide a layout list for a given type.
    /// </summary>
    public interface ILayoutProvider
    {
        /// <summary>
        /// Get the type corresponding to the layouts.
        /// </summary>
        Type LayoutFor { get; }

        /// <summary>
        /// Return the DataGridLayout for a type.
        /// </summary>
        /// <param name="dataType">Type corresponding to the layouts (equals or inherits LayoutFor).</param>
        /// <param name="layoutProviders">List of all the layout providers.</param>
        /// <param name="options">Dictionary of options for the layout definition.</param>
        /// <returns>The DataGridLayout.</returns>
        DataGridLayout GetDataGridLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options);

        /// <summary>
        /// Return the layout for a type.
        /// </summary>
        /// <param name="dataType">Type corresponding to the layouts (equals or inherits LayoutFor).</param>
        /// <param name="layoutProviders">List of all the layout providers.</param>
        /// <param name="options">Dictionary of options for the layout definition.</param>
        /// <returns>The type layout.</returns>
        Layout GetCreateUpdateLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options);
    }
}
