using System.Collections;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace SV.Db
{
    public interface IRecordFactory<T>
    {
        T Read(IDataReader reader);

        List<T> ReadBuffed(IDataReader reader, int estimateRow = 0);

        IEnumerable<T> ReadUnBuffed(IDataReader reader);
    }

    public abstract class RecordFactory<T> : IRecordFactory<T>
    {
        protected abstract void GenerateReadTokens(IDataReader reader, Span<int> tokens);

        protected abstract T Read(IDataReader reader, ref ReadOnlySpan<int> tokens);

        public virtual T Read(IDataReader reader)
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

        public virtual List<T> ReadBuffed(IDataReader reader, int estimateRow = 0)
        {
            List<T> results = new(estimateRow);
            if (reader.Read())
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
                    do
                    {
                        results.Add(Read(reader, ref readOnlyTokens));
                    }
                    while (reader.Read());
                    return results;
                }
                finally
                {
                    state.Dispose();
                }
            }
            return results;
        }

        public virtual IEnumerable<T> ReadUnBuffed(IDataReader reader)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = state.GetTokens();
            GenerateReadTokens(reader, s);
            return new UnBuffedEnumerator(reader, s, this, state);
        }

        internal unsafe struct UnBuffedEnumerator : IEnumerable<T>, IEnumerator<T>
        {
            private readonly IDataReader reader;
            private readonly RecordFactory<T> factory;
            private readonly ReaderState state;
            private readonly int* tokens;
            private readonly int length;

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public UnBuffedEnumerator(IDataReader reader, Span<int> span, RecordFactory<T> factory, ReaderState state)
            {
                this.reader = reader;
                this.factory = factory;
                this.state = state;
                fixed (int* ptr = &span.GetPinnableReference())
                {
                    tokens = ptr;
                    length = span.Length;
                }
            }

            public void Dispose()
            {
                state.Dispose();
            }

            public IEnumerator<T> GetEnumerator()
            {
                return this;
            }

            public bool MoveNext()
            {
                if (reader.Read())
                {
                    var s = new ReadOnlySpan<int>(tokens, length);
                    Current = factory.Read(reader, ref s);
                    return true;
                }
                return false;
            }

            public void Reset()
            {
                throw new NotImplementedException();
            }

            IEnumerator IEnumerable.GetEnumerator()
            {
                return this;
            }
        }

        public virtual IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = state.GetTokens();
            GenerateReadTokens(reader, s);
            return new UnBuffedAsyncEnumerator(reader, s, this, state);
        }

        internal struct UnBuffedAsyncEnumerator : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private readonly DbDataReader reader;
            private readonly RecordFactory<T> factory;
            private readonly ReaderState state;
            private readonly unsafe int* tokens;
            private readonly int length;

            public T Current { get; private set; }

            public UnBuffedAsyncEnumerator(DbDataReader reader, Span<int> span, RecordFactory<T> factory, ReaderState state)
            {
                this.reader = reader;
                this.factory = factory;
                this.state = state;
                unsafe
                {
                    fixed (int* ptr = &span.GetPinnableReference())
                    {
                        tokens = ptr;
                        length = span.Length;
                    }
                }
            }

            public ValueTask DisposeAsync()
            {
                state.Dispose();
                return ValueTask.CompletedTask;
            }

            public IAsyncEnumerator<T> GetAsyncEnumerator(CancellationToken cancellationToken = default)
            {
                return this;
            }

            public async ValueTask<bool> MoveNextAsync()
            {
                if (await reader.ReadAsync())
                {
                    Read();
                    return true;
                }
                return false;
            }

            private unsafe void Read()
            {
                var s = new ReadOnlySpan<int>(tokens, length);
                Current = factory.Read(reader, ref s);
            }
        }
    }
}