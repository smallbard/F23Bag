using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Common;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace F23Bag.ISeries.AdoProxies
{
    internal class ISeriesDbParameterCollection : DbParameterCollection
    {
        private List<DbParameter> _parameters = new List<DbParameter>();

        public override int Count => _parameters.Count;
        public override object SyncRoot { get; } = new object();

        public override int Add(object value)
        {
            _parameters.Add((DbParameter)value);
            return _parameters.Count - 1;
        }

        public override void AddRange(Array values)
        {
            _parameters.AddRange(values.OfType<DbParameter>());
        }

        public override void Clear() => _parameters.Clear();

        public override bool Contains(object value) => _parameters.Contains(value);

        public override bool Contains(string value) => _parameters.Any(p => p.ParameterName == value);

        public override void CopyTo(Array array, int index) => _parameters.CopyTo((DbParameter[])array, index);

        public override System.Collections.IEnumerator GetEnumerator() => _parameters.GetEnumerator();

        public override int IndexOf(object value) => _parameters.IndexOf(value as DbParameter);

        public override int IndexOf(string parameterName) => _parameters.Where(p => p.ParameterName == parameterName).Select((p, i) => i).First();

        public override void Insert(int index, object value) => _parameters.Insert(index, (DbParameter)value);

        public override void Remove(object value) => _parameters.Remove(value as DbParameter);

        public override void RemoveAt(int index) => _parameters.RemoveAt(index);

        public override void RemoveAt(string parameterName) => _parameters.Remove(_parameters.FirstOrDefault(p => p.ParameterName == parameterName));

        protected override DbParameter GetParameter(int index) => _parameters[index];

        protected override DbParameter GetParameter(string parameterName) => _parameters.FirstOrDefault(p => p.ParameterName == parameterName);

        protected override void SetParameter(int index, DbParameter value) => _parameters[index] = value;

        protected override void SetParameter(string parameterName, DbParameter value) => _parameters[IndexOf(parameterName)] = value;
    }
}
