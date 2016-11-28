using System.Collections.Generic;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Layout in the form of a grid.
    /// </summary>
    public class GridLayout : Layout
    {
        internal GridLayout(IEnumerable<ILayoutProvider> layoutProviders, IEnumerable<LayoutCellPosition> layoutCellPositions)
            : base(layoutProviders)
        {
            LayoutCellPositions = layoutCellPositions;
        }

        /// <summary>
        /// Get the list of cell definitions.
        /// </summary>
        public IEnumerable<LayoutCellPosition> LayoutCellPositions { get; private set; }
    }
}
