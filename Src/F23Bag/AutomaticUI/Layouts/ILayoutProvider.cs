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
        /// Return the layouts for the type.
        /// </summary>
        /// <param name="dataType">Type corresponding to the layouts (equals or inherits LayoutFor).</param>
        /// <param name="layoutProviders">List of all the layout providers.</param>
        /// <param name="options">Dictionary of options for the layout definition.</param>
        /// <returns>List of layouts for dataType.</returns>
        IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options);
    }
}
