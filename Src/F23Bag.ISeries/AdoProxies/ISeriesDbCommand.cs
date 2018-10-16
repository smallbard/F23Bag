using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text.RegularExpressions;

namespace F23Bag.ISeries.AdoProxies
{
    internal class ISeriesDbCommand : IDbCommand
    {
        private readonly IDbCommand _realCommand;

        public IDbConnection Connection { get; set; }
        public IDbTransaction Transaction { get => _realCommand.Transaction; set => _realCommand.Transaction = value; }
        public string CommandText { get; set; }
        public int CommandTimeout { get => _realCommand.CommandTimeout; set => _realCommand.CommandTimeout = value; }
        public CommandType CommandType { get => _realCommand.CommandType; set => _realCommand.CommandType = value; }
        public IDataParameterCollection Parameters { get; } = new ISeriesDbParameterCollection();
        public UpdateRowSource UpdatedRowSource { get => _realCommand.UpdatedRowSource; set => _realCommand.UpdatedRowSource = value; }

        public ISeriesDbCommand(IDbCommand realCommand, ISeriesDbConnection connection)
        {
            _realCommand = realCommand;
            Connection = connection;
        }

        public void Cancel() => _realCommand.Cancel();

        public IDbDataParameter CreateParameter() => _realCommand.CreateParameter();

        public void Dispose() => _realCommand.Dispose();

        public int ExecuteNonQuery()
        {
            SetCommandWithUnnamedParameters();
            return _realCommand.ExecuteNonQuery();
        }

        public IDataReader ExecuteReader()
        {
            SetCommandWithUnnamedParameters();
            return _realCommand.ExecuteReader();
        }

        public IDataReader ExecuteReader(CommandBehavior behavior)
        {
            SetCommandWithUnnamedParameters();
            return _realCommand.ExecuteReader(behavior);
        }

        public object ExecuteScalar()
        {
            SetCommandWithUnnamedParameters();
            return _realCommand.ExecuteScalar();
        }

        public void Prepare() => _realCommand.Prepare();

        private void SetCommandWithUnnamedParameters()
        {
            _realCommand.Parameters.Clear();

            _realCommand.CommandText = new Regex("@([a-zA-Z][a-zA-Z0-9_]*)").Replace(CommandText, m =>
            {
                var oldParam = Parameters.OfType<IDbDataParameter>().FirstOrDefault(p => p.ParameterName == m.Value);
                if (oldParam == null)
                {
                    return m.Value;
                }

                var newParam = CreateParameter();
                newParam.ParameterName = "@p" + _realCommand.Parameters.Count.ToString();
                newParam.Value = oldParam.Value;
                newParam.Direction = oldParam.Direction;
                newParam.DbType = oldParam.DbType;

                _realCommand.Parameters.Add(newParam);

                return "?";
            });
        }
    }
}
