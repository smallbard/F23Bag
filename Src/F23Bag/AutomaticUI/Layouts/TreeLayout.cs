using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace F23Bag.AutomaticUI.Layouts
{
    /// <summary>
    /// Layout in form of a tree.
    /// </summary>
    public class TreeLayout : Layout
    {
        internal TreeLayout(IEnumerable<ILayoutProvider> layoutProviders, MemberInfo children)
            : base(layoutProviders)
        {
            Children = children;
        }

        /// <summary>
        /// Get the membre wich gives the child of an object.
        /// </summary>
        public MemberInfo Children { get; private set; }
    }
}
