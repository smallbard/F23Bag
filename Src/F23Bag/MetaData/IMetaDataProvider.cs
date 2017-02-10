using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.MetaData
{
    public interface IMetaDataProvider
    {
        IQueryable<Table> Tables { get; }
    }
}
