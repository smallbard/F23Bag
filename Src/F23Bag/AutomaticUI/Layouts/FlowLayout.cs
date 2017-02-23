using System.Collections.Generic;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Layout in form of a horizontal or vertical flow.
    /// </summary>
    public class FlowLayout : Layout
    {
        internal FlowLayout(IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options, FlowDirectionEnum flowDirection, IEnumerable<Layout> childLayouts)
            : base(layoutProviders, options)
        {
            FlowDirection = flowDirection;
            ChildLayout = childLayouts;
        }

        /// <summary>
        /// Get the flow direction.
        /// </summary>
        public FlowDirectionEnum FlowDirection { get; private set; }

        /// <summary>
        /// Get the child layouts.
        /// </summary>
        public IEnumerable<Layout> ChildLayout { get; private set; }
    }

    public enum FlowDirectionEnum
    {
        Vertical,
        Horizontal
    }
}
