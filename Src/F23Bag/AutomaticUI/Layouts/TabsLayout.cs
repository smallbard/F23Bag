using System;
using System.Collections.Generic;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Layout in form of tabs.
    /// </summary>
    public class TabsLayout : Layout
    {
        internal TabsLayout(IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options, IEnumerable<Tuple<string, Layout>> tabs)
            : base(layoutProviders, options)
        {
            Tabs = tabs;
        }

        /// <summary>
        /// Get the tab list.
        /// </summary>
        public IEnumerable<Tuple<string, Layout>> Tabs { get; private set; }
    }
}
