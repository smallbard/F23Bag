using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class GenericObjectLayout : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(GenericObject<>);
            }
        }

        public DataGridLayout GetDataGridLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return null;
        }

        public Layout GetCreateUpdateLayout(Type dataType, IEnumerable<ILayoutProvider> layoutProviders, Dictionary<string, object> options)
        {
            return new LayoutBuilder<GenericObject<int>>(dataType, layoutProviders, options)
                .Grid(g => g
                    .Cell(0, 0, l => l.Vertical(v => v.Property(o => o.P1)))
                    .Cell(1, 0, 2, 4, l => l.Horizontal(h => { })))
                .GetLayout();
        }
    }
}
