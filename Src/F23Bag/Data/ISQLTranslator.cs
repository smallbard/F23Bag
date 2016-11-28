using System;
using System.Collections.Generic;
using F23Bag.Data.DML;

namespace F23Bag.Data
{
    public interface ISQLTranslator
    {
        string Translate(Request request, ICollection<Tuple<string, object>> parameters);        
    }
}
