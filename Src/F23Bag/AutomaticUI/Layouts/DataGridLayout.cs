using System.Collections.Generic;
using System.Reflection;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Layout in the form of a datagrid.
    /// </summary>
    public class DataGridLayout : Layout
    {
        internal DataGridLayout(IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options, IEnumerable<OneMemberLayout> columns, IEnumerable<OneMemberLayout> actions, MethodInfo openAction)
            : base(layoutProviders, options)
        {
            Columns = columns ?? new OneMemberLayout[] { };
            Actions = actions ?? new OneMemberLayout[] { };
            OpenAction = openAction;
        }

        /// <summary>
        /// Get the column collection.
        /// </summary>
        public IEnumerable<OneMemberLayout> Columns { get; private set; }

        /// <summary>
        /// Get the action collection.
        /// </summary>
        public IEnumerable<OneMemberLayout> Actions { get; private set; }

        /// <summary>
        /// Get the method used to open an element.
        /// </summary>
        public MethodInfo OpenAction { get; private set; }
    }
}
