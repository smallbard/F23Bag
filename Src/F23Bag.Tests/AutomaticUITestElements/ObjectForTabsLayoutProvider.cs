using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class ObjectForTabsLayoutProvider : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(ObjectForTabsLayout);
            }
        }

        public DataGridLayout GetDataGridLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return null;
        }

        public Layout GetCreateUpdateLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return new LayoutBuilder<ObjectForTabsLayout>(dataType, layoutProviders, options)
                .Tabs(t => t
                    .Tab("FirstTab", l => l
                        .Vertical(v => v
                            .Property(o => o.P1)
                            .Property(o => o.P3)))
                    .Tab("SecondTab", l => l
                        .Horizontal(h => h
                            .Property(o => o.P2)
                            .Property(o => o.P4))))
                .GetLayout();
        }
    }
}
