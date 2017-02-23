using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class ObjectForFlowLayoutProvider : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(ObjectForFlowLayout);
            }
        }

        public DataGridLayout GetDataGridLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return null;
        }

        public Layout GetCreateUpdateLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return new LayoutBuilder<ObjectForFlowLayout>(dataType, layoutProviders, options)
                .Horizontal(l => l.Property(o => o.P2).Property(o => o.P1))
                .GetLayout();
        }
    }
}
