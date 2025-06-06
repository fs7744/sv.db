﻿using System.Collections;
using System.Collections.Frozen;
using System.Data;
using System.Data.Common;
using System.Dynamic;
using System.Linq.Expressions;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Xml.Linq;

namespace SV.Db
{
    internal readonly struct DynamicRecordField
    {
        public DynamicRecordField(string name, Type type, string dataTypeName)
        {
            Name = name;
            Type = type;
            DataTypeName = dataTypeName;
        }

        public readonly Type Type;
        public readonly string Name, DataTypeName;
    }

    internal sealed class DynamicRecord : DbDataRecord, IReadOnlyDictionary<string, object?>, IDictionary<string, object?>,
    IDynamicMetaObjectProvider
    {
        private readonly DynamicRecordField[] fields;
        private readonly FrozenDictionary<string, int> keys;
        private readonly object[] values;

        public DynamicRecord(DynamicRecordField[] fields, IDataReader source, FrozenDictionary<string, int> keys)
        {
            this.fields = fields;
            this.keys = keys;
            values = new object[fields.Length];
            for (int i = 0; i < fields.Length; i++)
            {
                var r = source.GetValue(i);
                values[i] = r is DBNull ? null : r;
            }
        }

        public override int GetOrdinal(string name)
        {
            return keys[name];
        }

        public override int GetValues(object[] values)
        {
            var count = Math.Max(values.Length, FieldCount);
            Array.Copy(this.values, values, count);
            return count;
        }

        public override int FieldCount => fields.Length;

        public IEnumerable<string> Keys => keys.Keys;

        public IEnumerable<object> Values => values;

        public int Count => FieldCount;
        ICollection<string> IDictionary<string, object?>.Keys => keys.Keys;

        ICollection<object?> IDictionary<string, object?>.Values => values;

        bool ICollection<KeyValuePair<string, object?>>.IsReadOnly => true;

        object? IDictionary<string, object?>.this[string key]
        {
            get => this[key];
            set => values[keys[key]] = value;
        }

        public override string GetName(int i) => fields[i].Name;

        public override Type GetFieldType(int i) => fields[i].Type;

        protected override DbDataReader GetDbDataReader(int i) => throw new NotSupportedException();

        public override object GetValue(int i) => values[i];

        public override object this[string name]
        {
            get
            {
                return values[keys[name]];
            }
        }

        public override object this[int i] => values[i];

        public override bool GetBoolean(int i) => DBUtils.As<bool>(i);

        public override char GetChar(int i) => DBUtils.As<char>(i);

        public override string GetString(int i) => DBUtils.As<string>(i);

        public override byte GetByte(int i) => DBUtils.As<byte>(i);

        public override DateTime GetDateTime(int i) => DBUtils.As<DateTime>(i);

        public override decimal GetDecimal(int i) => DBUtils.As<decimal>(i);

        public override double GetDouble(int i) => DBUtils.As<double>(i);

        public override float GetFloat(int i) => DBUtils.As<float>(i);

        public override Guid GetGuid(int i) => DBUtils.As<Guid>(i);

        public override short GetInt16(int i) => DBUtils.As<short>(i);

        public override int GetInt32(int i) => DBUtils.As<int>(i);

        public override long GetInt64(int i) => DBUtils.As<long>(i);

        public override bool IsDBNull(int i) => values[i] is DBNull or null;

        public override string GetDataTypeName(int i) => fields[i].DataTypeName;

        private static int CheckOffsetAndComputeLength(int totalLength, long dataIndex, ref int length)
        {
            var offset = checked((int)dataIndex);
            var remaining = totalLength - offset;
            length = Math.Clamp(remaining, 0, length);
            return offset;
        }

        public override long GetBytes(int i, long dataIndex, byte[]? buffer, int bufferIndex, int length)
        {
            if (buffer is null) return 0;
            byte[] blob = (byte[])values[i];
            Buffer.BlockCopy(blob, CheckOffsetAndComputeLength(blob.Length, dataIndex, ref length), buffer, bufferIndex, length);
            return length;
        }

        public override long GetChars(int i, long dataIndex, char[]? buffer, int bufferIndex, int length)
        {
            if (buffer is null) return 0;
            if (values[i] is string s)
            {
                s.CopyTo(CheckOffsetAndComputeLength(s.Length, dataIndex, ref length), buffer, bufferIndex, length);
            }
            else
            {
                char[] clob = (char[])values[i];
                Array.Copy(clob, CheckOffsetAndComputeLength(clob.Length, dataIndex, ref length), buffer, bufferIndex, length);
            }
            return length;
        }

        public bool ContainsKey(string key) => keys.ContainsKey(key);

        public bool TryGetValue(string key, out object? value)
        {
            if (keys.TryGetValue(key, out var i))
            {
                value = values[i];
                return true;
            }
            value = null;
            return false;
        }

        public IEnumerator<KeyValuePair<string, object?>> GetEnumerator()
        {
            for (int i = 0; i < FieldCount; i++)
            {
                yield return new(fields[i].Name, values[i]);
            }
        }

        IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();

        void IDictionary<string, object?>.Add(string key, object? value) => throw new NotSupportedException();

        bool IDictionary<string, object?>.Remove(string key) => throw new NotSupportedException();

        void ICollection<KeyValuePair<string, object?>>.Add(KeyValuePair<string, object?> item) => throw new NotSupportedException();

        void ICollection<KeyValuePair<string, object?>>.Clear() => throw new NotSupportedException();

        bool ICollection<KeyValuePair<string, object?>>.Contains(KeyValuePair<string, object?> item)
        => TryGetValue(item.Key, out var value) && Equals(value, item.Value);

        void ICollection<KeyValuePair<string, object?>>.CopyTo(KeyValuePair<string, object?>[] array, int arrayIndex)
        {
            for (int i = 0; i < FieldCount; i++)
            {
                array[arrayIndex++] = new(fields[i].Name, values[i]);
            }
        }

        bool ICollection<KeyValuePair<string, object?>>.Remove(KeyValuePair<string, object?> item) => throw new NotSupportedException();

        DynamicMetaObject IDynamicMetaObjectProvider.GetMetaObject(Expression parameter)
            => new DynamicRecordMetaObject(parameter, BindingRestrictions.Empty, this);

        private sealed class DynamicRecordMetaObject : DynamicMetaObject
        {
            private static readonly MethodInfo getValueMethod;

            static DynamicRecordMetaObject()
            {
                IReadOnlyDictionary<string, object> tmp = new Dictionary<string, object> { { "", "" } };
                _ = tmp[""];
                getValueMethod = typeof(IReadOnlyDictionary<string, object>).GetProperty("Item")?.GetGetMethod()
                    ?? throw new InvalidOperationException("Unable to resolve indexer");
            }

            public DynamicRecordMetaObject(
            Expression expression,
            BindingRestrictions restrictions,
            object value
            )
            : base(expression, restrictions, value)
            {
            }

            private DynamicMetaObject CallMethod(
            MethodInfo method,
            Expression[] parameters
            )
            {
                var callMethod = new DynamicMetaObject(
                    Expression.Call(
                        Expression.Convert(Expression, LimitType),
                        method,
                        parameters),
                    BindingRestrictions.GetTypeRestriction(Expression, LimitType)
                    );
                return callMethod;
            }

            public override DynamicMetaObject BindGetMember(GetMemberBinder binder)
            {
                var parameters = new Expression[] { Expression.Constant(binder.Name) };

                var callMethod = CallMethod(getValueMethod, parameters);

                return callMethod;
            }

            public override DynamicMetaObject BindSetMember(SetMemberBinder binder, DynamicMetaObject value)
                => throw new NotSupportedException("Dynamic records are considered read-only currently");

            public override DynamicMetaObject BindInvokeMember(InvokeMemberBinder binder, DynamicMetaObject[] args)
            {
                var parameters = new Expression[]
                {
                Expression.Constant(binder.Name)
                };

                var callMethod = CallMethod(getValueMethod, parameters);

                return callMethod;
            }

            public override IEnumerable<string> GetDynamicMemberNames()
            {
                if (HasValue && Value is IDictionary<string, object> lookup) return lookup.Keys;
                return Array.Empty<string>();
            }
        }
    }

    public class DynamicRecordFactory<T> : RecordFactory<T> where T : class
    {
        protected override void GenerateReadTokens(DbDataReader reader, Span<int> tokens)
        {
            throw new NotImplementedException();
        }

        protected override T Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
        {
            throw new NotImplementedException();
        }

        public override T? Read(DbDataReader reader)
        {
            if (reader.Read())
            {
                var arr = new DynamicRecordField[reader.FieldCount];
                var dict = arr.Select((_, i) =>
                {
                    arr[i] = new DynamicRecordField(reader.GetName(i), reader.GetFieldType(i), reader.GetDataTypeName(i));
                    return new KeyValuePair<string, int>(arr[i].Name, i);
                }).ToFrozenDictionary();
                return (T)(object)new DynamicRecord(arr, reader, dict);
            }
            return default;
        }

        public override IEnumerable<T> ReadUnBuffed(DbDataReader reader)
        {
            var arr = new DynamicRecordField[reader.FieldCount];
            var dict = arr.Select((_, i) =>
            {
                arr[i] = new DynamicRecordField(reader.GetName(i), reader.GetFieldType(i), reader.GetDataTypeName(i));
                return new KeyValuePair<string, int>(arr[i].Name, i);
            }).ToFrozenDictionary();

            try
            {
                while (reader.Read())
                {
                    yield return (T)(object)new DynamicRecord(arr, reader, dict);
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        public override List<T?> ReadBuffed(DbDataReader reader, int estimateRow = 0)
        {
            var arr = new DynamicRecordField[reader.FieldCount];
            var dict = arr.Select((_, i) =>
            {
                arr[i] = new DynamicRecordField(reader.GetName(i), reader.GetFieldType(i), reader.GetDataTypeName(i));
                return new KeyValuePair<string, int>(arr[i].Name, i);
            }).ToFrozenDictionary();
            List<T?> list = new(estimateRow);
            try
            {
                while (reader.Read())
                {
                    list.Add((T)(object)new DynamicRecord(arr, reader, dict));
                }
                return list;
            }
            finally
            {
                reader.Dispose();
            }
        }

        public override async IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            var arr = new DynamicRecordField[reader.FieldCount];
            var dict = arr.Select((_, i) =>
            {
                arr[i] = new DynamicRecordField(reader.GetName(i), reader.GetFieldType(i), reader.GetDataTypeName(i));
                return new KeyValuePair<string, int>(arr[i].Name, i);
            }).ToFrozenDictionary();

            try
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    yield return (T)(object)new DynamicRecord(arr, reader, dict);
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        public override void SetParams(DbCommand cmd, object? args)
        {
            if (args != null && args is IEnumerable<KeyValuePair<string, object>> vs)
            {
                var ps = cmd.Parameters;
                foreach (var item in vs)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = item.Key;
                    p.Value = DBUtils.AsDBValue(item.Value);
                    p.DbType = RecordFactory.GetDbType(item.Value == null ? typeof(DBNull) : item.Value.GetType());
                    ps.Add(p);
                }
            }
        }

        public override void SetParams(DbBatchCommand cmd, object? args)
        {
            if (args is IEnumerable<KeyValuePair<string, object>> vs)
            {
                var ps = cmd.Parameters;
                foreach (var item in vs)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = item.Key;
                    p.Value = DBUtils.AsDBValue(item.Value);
                    p.DbType = RecordFactory.GetDbType(item.Value == null ? typeof(DBNull) : item.Value.GetType());
                    ps.Add(p);
                }
            }
        }

        public override void SetParams(IDbCmd cmd, T args)
        {
            if (args != null && args is IEnumerable<KeyValuePair<string, object>> vs)
            {
                var ps = cmd.Parameters;
                foreach (var item in vs)
                {
                    var p = cmd.CreateParameter();
                    p.ParameterName = item.Key;
                    p.Value = DBUtils.AsDBValue(item.Value);
                    p.DbType = RecordFactory.GetDbType(item.Value == null ? typeof(DBNull) : item.Value.GetType());
                    ps.Add(p);
                }
            }
        }
    }
}