using F23Bag.Data;
using System;
using System.Data;
using System.Data.Common;

namespace F23Bag.SQLite
{
    public class SQLiteProvider : IDisposable, ISQLProvider
    {
        private const string cstInMemoryConnectionString = "Data Source=:memory:;Version=3;New=True;";

        private readonly string _connectionString;
        private readonly DbConnection _inMemoryConnection;
        
        public SQLiteProvider(string connectionString)
        {
            _connectionString = connectionString;
        }

        public SQLiteProvider()
            : this(cstInMemoryConnectionString)
        {
            _inMemoryConnection = new System.Data.SQLite.SQLiteConnection(_connectionString);
        }

        public IDbConnection GetConnection()
        {
            return _inMemoryConnection != null ? new InMemoryConnection(_inMemoryConnection) : (DbConnection)new System.Data.SQLite.SQLiteConnection(_connectionString);
        }

        public ISQLTranslator GetSQLTranslator()
        {
            return new SQLiteSQLTranslator();
        }

        public IDDLTranslator GetDDLTranslator()
        {
            return new SQLiteDDLTranslator();
        }

        public void Dispose()
        {
            if (_inMemoryConnection != null) _inMemoryConnection.Dispose();
        }

        private class InMemoryConnection : DbConnection
        {
            private readonly DbConnection _connection;

            public InMemoryConnection(DbConnection connection)
            {
                _connection = connection;
            }

            public override string ConnectionString
            {
                get
                {
                    return _connection.ConnectionString;
                }

                set
                {
                    _connection.ConnectionString = value;
                }
            }

            public override string Database
            {
                get
                {
                    return _connection.Database;
                }
            }

            public override string DataSource
            {
                get
                {
                    return _connection.DataSource;
                }
            }

            public override string ServerVersion
            {
                get
                {
                    return _connection.ServerVersion;
                }
            }

            public override ConnectionState State
            {
                get
                {
                    return _connection.State;
                }
            }

            public override void ChangeDatabase(string databaseName)
            {
                _connection.ChangeDatabase(databaseName);
            }

            public override void Close()
            {
                
            }

            public override void Open()
            {
                _connection.Open();
            }

            protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
            {
                return _connection.BeginTransaction(isolationLevel);
            }

            protected override DbCommand CreateDbCommand()
            {
                return _connection.CreateCommand();
            }
        }
    }
}
