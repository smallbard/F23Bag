using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Tests.AutomaticUITestElements
{
    public class ObjectForTreeLayout
    {
        public IEnumerable<int> Children { get; } = new List<int> { 1, 2, 3 };
    }
}
