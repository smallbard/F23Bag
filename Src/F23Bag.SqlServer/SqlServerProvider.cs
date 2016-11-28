using F23Bag.Data;
using System.Data;
using System.Data.SqlClient;

namespace F23Bag.SqlServer
{
    public class SqlServerProvider : ISQLProvider
    {
        private readonly string _connectionString;

        public SqlServerProvider(string connectionstring)
        {
            _connectionString = connectionstring;
        }

        public IDbConnection GetConnection()
        {
            return new SqlConnection(_connectionString);
        }

        public IDDLTranslator GetDDLTranslator()
        {
            return new SqlServerDDLTranslator();
        }

        public ISQLTranslator GetSQLTranslator()
        {
            return new SqlServerSQLTranslator();
        }
    }
}
