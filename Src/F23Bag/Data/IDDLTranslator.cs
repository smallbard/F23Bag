using F23Bag.Data.DDL;
using System.Collections.Generic;

namespace F23Bag.Data
{
    public interface IDDLTranslator
    {
        void Translate(DDLStatement ddlStatement, ISQLMapping sqlMapping, List<string> objects, List<string> constraintsAndAlter);
    }
}
