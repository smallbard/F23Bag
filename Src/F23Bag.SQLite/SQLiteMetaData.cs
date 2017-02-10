using F23Bag.Data;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using F23Bag.Data.DML;
using System.Reflection;
using System.Collections;
using System.Data;
using F23Bag.MetaData;

namespace F23Bag.SQLite
{
    public class SQLiteMetaData : IMetaDataProvider
    {
        private readonly SQLiteProvider _provider;

        public SQLiteMetaData(SQLiteProvider provider)
        {
            _provider = provider;
        }

        public IQueryable<Table> Tables
        {
            get
            {
                var tables = new List<Table>();

                using (var connection = _provider.GetConnection())
                using (var cmd = connection.CreateCommand())
                {
                    if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                    cmd.CommandText = "SELECT name FROM sqlite_master WHERE type='table' AND NOT name LIKE 'sqlite%' ORDER BY name";

                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                        {
                            var tableName = (string)dr[0];
                            var table = new Table() { Name = tableName, Columns = new ColumnList(tableName, _provider, false), PrimaryKey = new ColumnList(tableName, _provider, true) };
                            table.ForeignKeys = new ForeignKeyList(table, _provider, tables);
                            tables.Add(table);
                        }
                }

                return tables.AsQueryable();
            }
        }

        private class SQLiteColumn : Column
        {
            private readonly string _tableName;

            public SQLiteColumn(string tableName)
            {
                _tableName = tableName;
            }

            public override bool Equals(object obj)
            {
                return obj is SQLiteColumn && ((SQLiteColumn)obj).Name == Name && ((SQLiteColumn)obj)._tableName == _tableName;
            }

            public override int GetHashCode()
            {
                return (_tableName + "." + Name).GetHashCode();
            }
        }

        private class ForeignKeyList : IList<ForeignKey>
        {
            private readonly Table _table;
            private readonly SQLiteProvider _provider;
            private readonly IEnumerable<Table> _tables;
            private bool _loaded;
            private List<ForeignKey> _foreignKeys;

            public ForeignKeyList(Table table, SQLiteProvider provider, IEnumerable<Table> tables)
            {
                _table = table;
                _provider = provider;
                _tables = tables;
            }

            public ForeignKey this[int index]
            {
                get
                {
                    LoadIfNeeded();
                    return _foreignKeys[index];
                }

                set
                {
                    LoadIfNeeded();
                    _foreignKeys[index] = value;
                }
            }

            public int Count
            {
                get
                {
                    LoadIfNeeded();
                    return _foreignKeys.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    return false;
                }
            }

            public void Add(ForeignKey item)
            {
                LoadIfNeeded();
                _foreignKeys.Add(item);
            }

            public void Clear()
            {
                LoadIfNeeded();
                _foreignKeys.Clear();
            }

            public bool Contains(ForeignKey item)
            {
                LoadIfNeeded();
                return _foreignKeys.Contains(item);
            }

            public void CopyTo(ForeignKey[] array, int arrayIndex)
            {
                LoadIfNeeded();
                _foreignKeys.CopyTo(array, arrayIndex);
            }

            public IEnumerator<ForeignKey> GetEnumerator()
            {
                LoadIfNeeded();
                return _foreignKeys.GetEnumerator();
            }

            public int IndexOf(ForeignKey item)
            {
                LoadIfNeeded();
                return _foreignKeys.IndexOf(item);
            }

            public void Insert(int index, ForeignKey item)
            {
                LoadIfNeeded();
                _foreignKeys.Insert(index, item);
            }

            public bool Remove(ForeignKey item)
            {
                LoadIfNeeded();
                return _foreignKeys.Remove(item);
            }

            public void RemoveAt(int index)
            {
                LoadIfNeeded();
                _foreignKeys.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return GetEnumerator();
            }

            private void LoadIfNeeded()
            {
                if (_loaded) return;

                _foreignKeys = new List<ForeignKey>();
                
                using (var connection = _provider.GetConnection())
                using (var cmd = connection.CreateCommand())
                {
                    if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                    //PRAGMA foreign_key_list(tableName) => id, seq, table, from, to, on_update, on_delete, match
                    cmd.CommandText = "PRAGMA foreign_key_list(" + _table.Name + ")";

                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read()) // todo : composite foreign key
                        {
                            var refTable = _tables.First(t => t.Name.Equals(Convert.ToString(dr["table"]), StringComparison.InvariantCultureIgnoreCase));
                            var fk = new ForeignKey()
                            {
                                ReferencedTable = refTable,
                                From = _table.Columns.Where(c => c.Name.Equals(Convert.ToString(dr["from"]), StringComparison.InvariantCultureIgnoreCase)).ToArray(),
                                To = refTable.Columns.Where(c => c.Name.Equals(Convert.ToString(dr["to"]), StringComparison.InvariantCultureIgnoreCase)).ToArray()
                            };
                            _foreignKeys.Add(fk);
                        }
                }

                _loaded = true;
            }
        }

        private class ColumnList : IList<Column>
        {
            private readonly string _tableName;
            private readonly SQLiteProvider _provider;
            private readonly bool _primaryKeyOnly;
            private bool _loaded;
            private List<Column> _columns;

            public ColumnList(string tableName, SQLiteProvider provider, bool primaryKeyOnly)
            {
                _tableName = tableName;
                _provider = provider;
                _primaryKeyOnly = primaryKeyOnly;
            }

            public Column this[int index]
            {
                get
                {
                    LoadIfNeeded();
                    return _columns[index];
                }

                set
                {
                    LoadIfNeeded();
                    _columns[index] = value;
                }
            }

            public int Count
            {
                get
                {
                    LoadIfNeeded();
                    return _columns.Count;
                }
            }

            public bool IsReadOnly
            {
                get
                {
                    LoadIfNeeded();
                    return false;
                }
            }

            public void Add(Column item)
            {
                LoadIfNeeded();
                _columns.Add(item);
            }

            public void Clear()
            {
                LoadIfNeeded();
                _columns.Clear();
            }

            public bool Contains(Column item)
            {
                LoadIfNeeded();
                return _columns.Contains(item);
            }

            public void CopyTo(Column[] array, int arrayIndex)
            {
                LoadIfNeeded();
                _columns.CopyTo(array, arrayIndex);
            }

            public IEnumerator<Column> GetEnumerator()
            {
                LoadIfNeeded();
                return _columns.GetEnumerator();
            }

            public int IndexOf(Column item)
            {
                LoadIfNeeded();
                return _columns.IndexOf(item);
            }

            public void Insert(int index, Column item)
            {
                LoadIfNeeded();
                _columns.Insert(index, item);
            }

            public bool Remove(Column item)
            {
                LoadIfNeeded();
                return _columns.Remove(item);
            }

            public void RemoveAt(int index)
            {
                LoadIfNeeded();
                _columns.RemoveAt(index);
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                LoadIfNeeded();
                return _columns.GetEnumerator();
            }

            private void LoadIfNeeded()
            {
                if (_loaded) return;

                _columns = new List<Column>();

                using (var connection = _provider.GetConnection())
                using (var cmd = connection.CreateCommand())
                {
                    if (connection.State != System.Data.ConnectionState.Open) connection.Open();

                    cmd.CommandText = "PRAGMA table_info(" + _tableName + ")";

                    using (var dr = cmd.ExecuteReader())
                        while (dr.Read())
                        {
                            if (_primaryKeyOnly && Convert.ToInt32(dr["pk"]) == 0) continue;
                            int? length;
                            var column = new SQLiteColumn(_tableName) { Name = (string)dr["name"], ColumnType = GetSqlDbType((string)dr["type"], out length), IsNullable = 0 == Convert.ToInt32(dr["notnull"]) };
                            column.Length = length;
                            _columns.Add(column);
                        }
                }

                _loaded = true;
            }

            private SqlDbType GetSqlDbType(string typeName, out int? length)
            {
                length = null;
                var pIndex = typeName.IndexOf('(');
                if (pIndex > 0)
                {
                    var strLength = typeName.Substring(pIndex + 1, typeName.IndexOf(')') - pIndex - 1);
                    if (strLength.ToLower().Contains("max"))
                        length = 1000000000;
                    else
                        length = Convert.ToInt32(strLength);
                    typeName = typeName.Substring(0, pIndex);
                }

                if (typeName.Equals("int", StringComparison.InvariantCultureIgnoreCase) || typeName.Equals("integer", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Int;
                else if (typeName.Equals("tinyint", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.TinyInt;
                else if (typeName.Equals("smallint", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.SmallInt;
                else if (typeName.Equals("mediumint", StringComparison.InvariantCultureIgnoreCase) || typeName.Equals("int2", StringComparison.InvariantCultureIgnoreCase) || typeName.Equals("int8", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Int;
                else if (typeName.Equals("bigint", StringComparison.InvariantCultureIgnoreCase) || typeName.Equals("unsigned big int", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.BigInt;
                else if (typeName.Equals("character", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Char;
                else if (typeName.Equals("varchar", StringComparison.InvariantCultureIgnoreCase) || typeName.Equals("varyning character", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.VarChar;
                else if (typeName.Equals("nchar", StringComparison.InvariantCultureIgnoreCase) || typeName.Equals("native character", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.NChar;
                else if (typeName.Equals("nvarchar", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.NVarChar;
                else if (typeName.Equals("text", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Text;
                else if (typeName.Equals("blob", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Binary;
                else if (typeName.Equals("real", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Real;
                else if (typeName.Equals("double", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Real;
                else if (typeName.Equals("double precision", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Real;
                else if (typeName.Equals("float", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Float;
                else if (typeName.Equals("numeric", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Decimal;
                else if (typeName.Equals("decimal", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Decimal;
                else if (typeName.Equals("boolean", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Int;
                else if (typeName.Equals("date", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.Date;
                else if (typeName.Equals("datetime", StringComparison.InvariantCultureIgnoreCase))
                    return SqlDbType.DateTime2;

                throw new NotSupportedException($"Not supported datatype {typeName}");
            }
        }
    }
}
