using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Winforms.Tests
{
    public class Test1Layout : ILayoutProvider
    {
        public IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return new LayoutBuilder<Test1>(dataType, layoutProviders)
                .Grid(gb => gb
                    .Cell(0, 0, fb => fb.Vertical(v => v.Property(t1 => t1.Prop1).Property(t1 => t1.Prop2).Property(t1 => t1.StringFromList, null, t1 => t1.ListForString)))
                    .Cell(1, 0, fb => fb.Tabs(tbl =>
                        tbl
                            .Tab("FirstTab", t => t.Vertical(v => v.Property(t1 => t1.Prop3).Property(t1 => t1.Prop4)))
                            .Tab("SecondTab", t => t.Vertical(v => v.Property(t1 => t1.Prop5).Method(t1 => (Func<Test2>)t1.Test)))))
                    .Cell(0, 1, fb => fb.Vertical(v => v.Property(t1 => t1.EnumValue)))
                    .Cell(1, 1, fb => fb.Vertical(v => v.Property(t1 => t1.EnumValueNotNull)))
                    .Cell(0, 2, fb => fb.Vertical(v => v.Property(t1 => t1.Tests))))
                .GetLayouts();
        }

        public Type LayoutFor
        {
            get
            {
                return typeof(Test1);
            }
        }
    }
}
