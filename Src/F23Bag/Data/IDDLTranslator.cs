using F23Bag.Data.DDL;
using System.Collections.Generic;

namespace F23Bag.Data
{
    public interface IDDLTranslator
    {
        IEnumerable<string> Translate(DDLStatement ddlStatement, ISQLMapping sqlMapping);
    }
}
