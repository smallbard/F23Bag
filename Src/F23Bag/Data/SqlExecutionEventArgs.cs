using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.Data
{
    public class SqlExecutionEventArgs
    {
        public SqlExecutionEventArgs(string sql, Dictionary<string,object> parameters)
        {
            Sql = sql;
            Parameters = parameters;
        }

        public string Sql { get; }

        public Dictionary<string, object> Parameters { get; }
    }
}
