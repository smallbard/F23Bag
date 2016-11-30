using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Domain
{
    public interface IDataAccess<T>
    {
        IQueryable<T> GetQuery();

        void Save(T o);
    }
}
