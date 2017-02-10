using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.MetaData
{
    public class ForeignKey
    {
        public IList<Column> From { get; set; }

        public Table ReferencedTable { get; set; }

        public IList<Column> To { get; set; }
    }
}
