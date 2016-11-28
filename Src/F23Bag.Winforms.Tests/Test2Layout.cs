using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Winforms.Tests
{
    public class Test2Layout : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(Test2);
            }
        }

        public IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        { 
                return new LayoutBuilder<Test2>(dataType, layoutProviders)
                    .DataGrid(dg => dg
                        .Column(t2 => t2.PropInt)
                        .Column(t2 => t2.Prop2)
                        .Column(t2 => t2.EnumValue)
                        .Column(t2 => (Action<Test3, Test4>)t2.Test, "click!")
                        .Action(t2 => (Action<Test3, Test4>)t2.Test, "Action 1")
                        .Action(t2 => (Action<Test3, Test4>)t2.Test, "Action2"))
                    .Grid(g => g
                        .Cell(0, 0, l => l.Property(t2 => t2.PropInt))
                        .Cell(0, 1, l => l.Property(t2 => t2.Prop2))
                        .Cell(1, 0, l => l.Property(t2 => t2.EnumValue))
                        .Cell(1, 1, l => l.Property(t2 => t2.EnumValueNotNull))
                        .Cell(0, 2, l => l.Method(t2 => (Action<Test3, Test4>)t2.Test, null, true)))
                    .GetLayouts();
        }
    }
}
