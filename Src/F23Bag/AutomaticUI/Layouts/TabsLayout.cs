using System;
using System.Collections.Generic;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Layout in form of tabs.
    /// </summary>
    public class TabsLayout : Layout
    {
        internal TabsLayout(IEnumerable<ILayoutProvider> layoutProviders, IEnumerable<Tuple<string, Layout>> tabs)
            : base(layoutProviders)
        {
            Tabs = tabs;
        }

        /// <summary>
        /// Get the tab list.
        /// </summary>
        public IEnumerable<Tuple<string, Layout>> Tabs { get; private set; }
    }
}
