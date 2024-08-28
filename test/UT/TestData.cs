using System.Collections;
using System.Data;
using System.Data.Common;
using System.Diagnostics.CodeAnalysis;

#pragma warning disable CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).

namespace UT
{
    public class TestDbConnection : DbConnection
    {
        public override string? ConnectionString { get; set; }

        public override string? Database { get; }

        public override string? DataSource { get; }

        public override string? ServerVersion { get; }

        public override ConnectionState State { get; }
        public TestData? Data { get; set; }
        public int RowCount { get; set; }

        public override void ChangeDatabase(string databaseName)
        {
        }

        public override void Close()
        {
        }

        public override void Open()
        {
        }

        protected override DbTransaction BeginDbTransaction(IsolationLevel isolationLevel)
        {
            throw new NotImplementedException();
        }

        protected override DbCommand CreateDbCommand()
        {
            return new TestDbCommand() { Data = Data, RowCount = RowCount, Connection = this };
        }
    }

    public class TestDbCommand : DbCommand
    {
        public override string? CommandText { get; set; }
        public override int CommandTimeout { get; set; }
        public override CommandType CommandType { get; set; }
        public override bool DesignTimeVisible { get; set; }
        public override UpdateRowSource UpdatedRowSource { get; set; }
        public TestData? Data { get; set; }
        public int RowCount { get; set; }
        protected override DbConnection? DbConnection { get; set; }

        protected override DbParameterCollection DbParameterCollection { get; } = new TestDataParameterCollection();

        protected override DbTransaction? DbTransaction { get; set; }

        public override void Cancel()
        {
        }

        public override int ExecuteNonQuery()
        {
            throw new NotImplementedException();
        }

        public override object? ExecuteScalar()
        {
            return new TestDbDataReader(Data) { RowCount = RowCount }.GetValue(0);
        }

        public override void Prepare()
        {
        }

        protected override DbParameter CreateDbParameter()
        {
            throw new NotImplementedException();
        }

        protected override DbDataReader ExecuteDbDataReader(CommandBehavior behavior)
        {
            return new TestDbDataReader(Data) { RowCount = RowCount };
        }
    }

    public class TestDataParameterCollection : DbParameterCollection
    {
        public override int Count { get; }

        public override object? SyncRoot { get; }

        public override int Add(object value)
        {
            throw new NotImplementedException();
        }

        public override void AddRange(Array values)
        {
            throw new NotImplementedException();
        }

        public override void Clear()
        {
        }

        public override bool Contains(object value)
        {
            throw new NotImplementedException();
        }

        public override bool Contains(string value)
        {
            throw new NotImplementedException();
        }

        public override void CopyTo(Array array, int index)
        {
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        public override int IndexOf(object value)
        {
            throw new NotImplementedException();
        }

        public override int IndexOf(string parameterName)
        {
            throw new NotImplementedException();
        }

        public override void Insert(int index, object value)
        {
            throw new NotImplementedException();
        }

        public override void Remove(object value)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(int index)
        {
            throw new NotImplementedException();
        }

        public override void RemoveAt(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override DbParameter GetParameter(int index)
        {
            throw new NotImplementedException();
        }

        protected override DbParameter GetParameter(string parameterName)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(int index, DbParameter value)
        {
            throw new NotImplementedException();
        }

        protected override void SetParameter(string parameterName, DbParameter value)
        {
            throw new NotImplementedException();
        }
    }

    public class TestDataParameter : DbParameter
    {
        public override DbType DbType { get; set; }
        public override ParameterDirection Direction { get; set; }
        public override bool IsNullable { get; set; }
        public override string? ParameterName { get; set; }
        public override int Size { get; set; }
        public override string? SourceColumn { get; set; }
        public override bool SourceColumnNullMapping { get; set; }
        public override object? Value { get; set; }

        public override void ResetDbType()
        {
        }
    }

    public class TestData
    {
        internal readonly string[] nameIndexs;
        internal readonly Dictionary<string, int> names = new Dictionary<string, int>();
        internal readonly Dictionary<string, object> kvs = new Dictionary<string, object>();
        internal readonly object[] values;
        internal readonly string[] strings;
        internal readonly bool[] bools;
        internal readonly byte[] bytes;
        internal readonly char[] chars;
        internal readonly decimal[] decimals;
        internal readonly DateTime[] DateTimes;
        internal readonly double[] doubles;
        internal readonly float[] floats;
        internal readonly Guid[] Guids;
        internal readonly short[] shorts;
        internal readonly int[] ints;
        internal readonly long[] longs;
        internal readonly DBNull?[] dbNulls;

        public TestData(params (string name, object value)[] fields)
        {
            nameIndexs = new string[fields.Length];
            values = new object[fields.Length];
            strings = new string[fields.Length];
            bools = new bool[fields.Length];
            bytes = new byte[fields.Length];
            chars = new char[fields.Length];
            decimals = new decimal[fields.Length];
            DateTimes = new DateTime[fields.Length];
            doubles = new double[fields.Length];
            floats = new float[fields.Length];
            Guids = new Guid[fields.Length];
            shorts = new short[fields.Length];
            ints = new int[fields.Length];
            longs = new long[fields.Length];
            dbNulls = new DBNull?[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                var v = fields[i];
                kvs.Add(v.name, v.value);
                names.Add(v.name, i);
                nameIndexs[i] = v.name;
                values[i] = v.value;
                if (values[i] is null)
                {
                    values[i] = DBNull.Value;
                    dbNulls[i] = DBNull.Value;
                }

                var t = values[i].GetType();
                if (t == typeof(string))
                {
                    strings[i] = (string)values[i];
                }
                else if (t == typeof(bool))
                {
                    bools[i] = (bool)values[i];
                }
                else if (t == typeof(byte))
                {
                    bytes[i] = (byte)values[i];
                }
                else if (t == typeof(char))
                {
                    chars[i] = (char)values[i];
                }
                else if (t == typeof(DateTime))
                {
                    DateTimes[i] = (DateTime)values[i];
                }
                else if (t == typeof(decimal))
                {
                    decimals[i] = (decimal)values[i];
                }
                else if (t == typeof(double))
                {
                    doubles[i] = (double)values[i];
                }
                else if (t == typeof(float))
                {
                    floats[i] = (float)values[i];
                }
                else if (t == typeof(Guid))
                {
                    Guids[i] = (Guid)values[i];
                }
                else if (t == typeof(short))
                {
                    shorts[i] = (short)values[i];
                }
                else if (t == typeof(int))
                {
                    ints[i] = (int)values[i];
                }
                else if (t == typeof(long))
                {
                    longs[i] = (long)values[i];
                }
            }
        }
    }

    public class TestDbDataReader : DbDataReader
    {
        private readonly TestData? data;
        public int RowCount { get; set; }
        private int calls = 0;

        public TestDbDataReader(TestData? data)
        {
            this.data = data;
        }

        public override object this[int ordinal] => throw new NotImplementedException();

        public override object this[string name] => data.kvs[name];

        public override int Depth { get; }

        public override int FieldCount => data.kvs.Count;

        public override bool HasRows => throw new NotImplementedException();

        public override bool IsClosed => throw new NotImplementedException();

        public override int RecordsAffected => RowCount;

        public override bool GetBoolean(int ordinal)
        {
            return data.bools[ordinal];
        }

        public override byte GetByte(int ordinal)
        {
            return data.bytes[ordinal];
        }

        public override long GetBytes(int ordinal, long dataOffset, byte[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override char GetChar(int ordinal)
        {
            return data.chars[ordinal];
        }

        public override long GetChars(int ordinal, long dataOffset, char[]? buffer, int bufferOffset, int length)
        {
            throw new NotImplementedException();
        }

        public override string GetDataTypeName(int ordinal)
        {
            return GetFieldType(ordinal).ToString();
        }

        public override DateTime GetDateTime(int ordinal)
        {
            return data.DateTimes[ordinal];
        }

        public override decimal GetDecimal(int ordinal)
        {
            return data.decimals[ordinal];
        }

        public override double GetDouble(int ordinal)
        {
            return data.doubles[ordinal];
        }

        public override IEnumerator GetEnumerator()
        {
            throw new NotImplementedException();
        }

        [return: DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicFields | DynamicallyAccessedMemberTypes.PublicProperties)]
        public override Type GetFieldType(int ordinal)
        {
            return GetValue(ordinal).GetType();
        }

        public override float GetFloat(int ordinal)
        {
            return data.floats[ordinal];
        }

        public override Guid GetGuid(int ordinal)
        {
            return data.Guids[ordinal];
        }

        public override short GetInt16(int ordinal)
        {
            return data.shorts[ordinal];
        }

        public override int GetInt32(int ordinal)
        {
            return data.ints[ordinal];
        }

        public override long GetInt64(int ordinal)
        {
            return data.longs[ordinal];
        }

        public override string GetName(int ordinal)
        {
            return data.nameIndexs[ordinal];
        }

        public override int GetOrdinal(string name)
        {
            return data.names[name];
        }

        public override string GetString(int ordinal)
        {
            return data.strings[ordinal];
        }

        public override object GetValue(int ordinal)
        {
            return data.values[ordinal];
        }

        public override int GetValues(object[] values)
        {
            Array.Copy(data.values, values, data.values.Length);
            return data.values.Length;
        }

        public override bool IsDBNull(int ordinal)
        {
            return data.dbNulls[ordinal] != null;
        }

        public override bool NextResult()
        {
            return false;
        }

        public override bool Read()
        {
            calls++;
            return calls <= RowCount;
        }
    }
}

#pragma warning restore CS8764 // Nullability of return type doesn't match overridden member (possibly because of nullability attributes).