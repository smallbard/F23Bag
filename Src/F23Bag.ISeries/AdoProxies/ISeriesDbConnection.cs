using System.Data;

namespace F23Bag.ISeries.AdoProxies
{
    internal class ISeriesDbConnection : IDbConnection
    {
        private readonly IDbConnection _realConnection;

        public string ConnectionString { get => _realConnection.ConnectionString; set => _realConnection.ConnectionString = value; }
        public int ConnectionTimeout => _realConnection.ConnectionTimeout;
        public string Database => _realConnection.Database; 
        public ConnectionState State => _realConnection.State; 

        public ISeriesDbConnection(IDbConnection realConnection)
        {
            _realConnection = realConnection;
        }

        public IDbTransaction BeginTransaction() => _realConnection.BeginTransaction();

        public IDbTransaction BeginTransaction(IsolationLevel il) => _realConnection.BeginTransaction(il);

        public void ChangeDatabase(string databaseName) => _realConnection.ChangeDatabase(databaseName);

        public void Close() => _realConnection.Close();

        public IDbCommand CreateCommand()
        {
            return new ISeriesDbCommand(_realConnection.CreateCommand(), this);
        }

        public void Dispose() => _realConnection.Dispose();

        public void Open() => _realConnection.Open();
    }
}
