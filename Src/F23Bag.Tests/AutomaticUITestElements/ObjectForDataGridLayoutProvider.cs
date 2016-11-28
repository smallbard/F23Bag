using F23Bag.AutomaticUI.Layouts;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class ObjectForDataGridLayoutProvider : ILayoutProvider
    {
        public Type LayoutFor
        {
            get
            {
                return typeof(ObjectForDataGridLayout);
            }
        }

        public IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return new LayoutBuilder<ObjectForDataGridLayout>(dataType, layoutProviders)
                .DataGrid(dg => dg
                    .Column(o => o.P1)
                    .Column(o => o.P3)
                    .Column(o => o.P2)
                    .Action(o => (Action)o.A1, "Action 1")
                    .Open(o => (Action)o.Open))
                .GetLayouts();
        }
    }
}
