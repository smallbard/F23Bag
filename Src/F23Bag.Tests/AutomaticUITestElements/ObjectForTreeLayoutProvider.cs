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

        public IEnumerable<Layout> GetLayouts(Type dataType, IEnumerable<ILayoutProvider> layoutProviders)
        {
            return new LayoutBuilder<ObjectForTreeLayout>(dataType, layoutProviders)
                .Tree(tb => tb.Children(o => o.Children))
                .GetLayouts();
        }
    }
}
