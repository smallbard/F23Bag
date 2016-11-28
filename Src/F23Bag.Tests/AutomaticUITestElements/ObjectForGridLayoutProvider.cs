using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class ObjectForGridLayoutProvider : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(ObjectForGridLayout);
            }
        }

        public IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return new LayoutBuilder<ObjectForGridLayout>(dataType, layoutProviders)
                .Grid(g => g
                    .Cell(0,0, l => l.Vertical(v => v
                        .Property(o => o.P1)
                        .Property(o => o.P3)))
                    .Cell(1,0,2,4, l => l.Horizontal(v => v
                        .Property(o => o.P2)
                        .Property(o => o.P4)
                        .Property<ObjectInheritsGridLayout>(o => o.PH))))
                .GetLayouts();
        }
    }
}
