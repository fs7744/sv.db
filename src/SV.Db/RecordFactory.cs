using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace SV.Db
{
    public abstract class RecordFactory<T>
    {
        protected abstract void GenerateReadTokens(DbDataReader reader, Span<int> tokens);

        protected abstract T Read(IDataReader reader, ref ReadOnlySpan<int> tokens);

        public virtual T Read(DbDataReader reader)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = reader.FieldCount <= 64 ? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(stackalloc int[reader.FieldCount]), reader.FieldCount) : state.GetTokens();
            GenerateReadTokens(reader, s);
            ReadOnlySpan<int> readOnlyTokens = s;
            return Read(reader, ref readOnlyTokens);
        }

        public virtual List<T> ReadBuffed(DbDataReader reader)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = reader.FieldCount <= 64 ? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(stackalloc int[reader.FieldCount]), reader.FieldCount) : state.GetTokens();
            GenerateReadTokens(reader, s);
            ReadOnlySpan<int> readOnlyTokens = s;
            List<T> results = [];
            try
            {
                while (reader.Read())
                {
                    results.Add(Read(reader, ref readOnlyTokens));
                }
                return results;
            }
            finally
            {
                state.Dispose();
            }
        }

        public virtual IEnumerable<T> ReadUnBuffed(DbDataReader reader)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = reader.FieldCount <= 64 ? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(stackalloc int[reader.FieldCount]), reader.FieldCount) : state.GetTokens();
            GenerateReadTokens(reader, s);
            ReadOnlySpan<int> readOnlyTokens = s;
            try
            {
                while (reader.Read())
                {
                    yield return Read(reader, ref readOnlyTokens);
                }
            }
            finally
            {
                state.Dispose();
            }
        }
    }
}