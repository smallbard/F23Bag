using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    /// <summary>
    /// Implemented by types which are stored as simple types in database.
    /// </summary>
    /// <typeparam name="TEquivalent">Type used to store in database.</typeparam>
    /// <remarks>
    /// Classes with <see cref="IDbValueType{TEquivalent}"/> must have a constructor with one parameter of type <see cref="TEquivalent"/>.
    /// </remarks>
    public interface IDbValueType<TEquivalent>
    {
        TEquivalent GetDbValue();
    }
}
