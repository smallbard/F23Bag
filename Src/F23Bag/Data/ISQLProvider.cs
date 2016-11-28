using System.Data;

namespace F23Bag.Data
{
    public interface ISQLProvider
    {
        IDbConnection GetConnection();

        ISQLTranslator GetSQLTranslator();

        IDDLTranslator GetDDLTranslator();
    }
}