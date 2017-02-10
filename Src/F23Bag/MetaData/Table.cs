using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.MetaData
{
    public class Table
    {
        public virtual string Name { get; set; }

        public IList<Column> Columns { get; set; }

        public IList<Column> PrimaryKey { get; set; }

        public IList<ForeignKey> ForeignKeys { get; set; }
    }
}
