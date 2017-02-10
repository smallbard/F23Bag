using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.MetaData
{
    public class Column
    {
        public virtual string Name { get; set; }

        public virtual bool IsNullable { get; set; }

        public virtual SqlDbType ColumnType { get; set; }

        public int? Length { get; set; }
    }
}
