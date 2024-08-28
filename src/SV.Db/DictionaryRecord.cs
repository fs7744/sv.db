using System.Collections.Concurrent;
using System.Data.Common;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    public abstract class IDictionaryRecord<T> : RecordFactory<T> where T : IDictionary<string, object?>
    {
        public override void SetParams(DbCommand cmd, object args)
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

        public override void SetParams(DbBatchCommand cmd, T args)
        {
            SetParams(cmd, (object)args);
        }

        public override void SetParams(DbBatchCommand cmd, object args)
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

        public override void SetParams(DbCommand cmd, T args)
        {
            SetParams(cmd, (object)args);
        }

        protected override void GenerateReadTokens(DbDataReader reader, Span<int> tokens)
        {
            throw new NotImplementedException();
        }

        protected override T Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
        {
            throw new NotImplementedException();
        }

        public abstract T Create();

        public override T? Read(DbDataReader reader)
        {
            if (reader.Read())
            {
                T dict = ReadDict(reader);
                return dict;
            }
            return default;
        }

        private T ReadDict(DbDataReader reader)
        {
            var dict = Create();
            for (int i = 0; i < reader.FieldCount; i++)
            {
                var v = reader.GetValue(i);
                dict[reader.GetName(i)] = v == DBNull.Value ? null : v;
            }

            return dict;
        }

        public override List<T?> ReadBuffed(DbDataReader reader, int estimateRow = 0)
        {
            List<T?> list = new(estimateRow);
            try
            {
                while (reader.Read())
                {
                    list.Add(ReadDict(reader));
                }
                return list;
            }
            finally
            {
                reader.Dispose();
            }
        }

        public override IEnumerable<T> ReadUnBuffed(DbDataReader reader)
        {
            try
            {
                while (reader.Read())
                {
                    yield return ReadDict(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }
        }

        public override async IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader, [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            try
            {
                while (await reader.ReadAsync(cancellationToken))
                {
                    yield return ReadDict(reader);
                }
            }
            finally
            {
                reader.Dispose();
            }
        }
    }

    public class DictionaryRecord : IDictionaryRecord<Dictionary<string, object?>>
    {
        public override Dictionary<string, object?> Create()
        {
            return new Dictionary<string, object?>();
        }
    }

    public class ConcurrentDictionaryRecord : IDictionaryRecord<ConcurrentDictionary<string, object?>>
    {
        public override ConcurrentDictionary<string, object?> Create()
        {
            return new ConcurrentDictionary<string, object?>();
        }
    }
}