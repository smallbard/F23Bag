using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class ObjectForTreeLayoutProvider : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(ObjectForTreeLayout);
            }
        }

        public DataGridLayout GetDataGridLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return null;
        }

        public Layout GetCreateUpdateLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return new LayoutBuilder<ObjectForTreeLayout>(dataType, layoutProviders, options)
                .Tree(tb => tb.Children(o => o.Children))
                .GetLayout();
        }
    }
}
