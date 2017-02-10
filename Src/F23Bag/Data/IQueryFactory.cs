using F23Bag.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    public interface IQueryFactory
    {
        IQueryable<T> GetQuery<T>();
    }
}
