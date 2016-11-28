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

        public IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return new LayoutBuilder<GenericObject<int>>(dataType, layoutProviders)
                .Grid(g => g
                    .Cell(0, 0, l => l.Vertical(v => v.Property(o => o.P1)))
                    .Cell(1, 0, 2, 4, l => l.Horizontal(h => { })))
                .GetLayouts();
        }
    }
}
