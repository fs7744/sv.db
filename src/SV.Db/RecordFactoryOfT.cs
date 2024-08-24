using System.Collections;
using System.Data.Common;
using System.Runtime.InteropServices;

namespace SV.Db
{
    public interface IParamsSetter
    {
        void SetParams(DbCommand cmd, object args);
    }

    public interface IParamsSetter<T> : IParamsSetter
    {
        void SetParams(DbCommand cmd, T args);
    }

    public interface IRecordFactory<T>
    {
        T Read(DbDataReader reader);

        List<T> ReadBuffed(DbDataReader reader, int estimateRow = 0);

        IEnumerable<T> ReadUnBuffed(DbDataReader reader);

        IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader, CancellationToken cancellationToken = default);
    }

    public abstract class RecordFactory<T> : IRecordFactory<T>, IParamsSetter<T>
    {
        public abstract void SetParams(DbCommand cmd, object args);

        public abstract void SetParams(DbCommand cmd, T args);

        protected abstract void GenerateReadTokens(DbDataReader reader, Span<int> tokens);

        protected abstract T Read(DbDataReader reader, ref ReadOnlySpan<int> tokens);

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

        public virtual List<T> ReadBuffed(DbDataReader reader, int estimateRow = 0)
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

        public virtual IEnumerable<T> ReadUnBuffed(DbDataReader reader)
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
            private readonly DbDataReader reader;
            private readonly RecordFactory<T> factory;
            private readonly ReaderState state;
            private readonly int* tokens;
            private readonly int length;

            public T Current { get; private set; }

            object IEnumerator.Current => Current;

            public UnBuffedEnumerator(DbDataReader reader, Span<int> span, RecordFactory<T> factory, ReaderState state)
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

        public virtual IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader, CancellationToken cancellationToken = default)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = state.GetTokens();
            GenerateReadTokens(reader, s);
            return new UnBuffedAsyncEnumerator(reader, s, this, state, ref cancellationToken);
        }

        internal struct UnBuffedAsyncEnumerator : IAsyncEnumerable<T>, IAsyncEnumerator<T>
        {
            private readonly DbDataReader reader;
            private readonly RecordFactory<T> factory;
            private readonly ReaderState state;
            private readonly unsafe int* tokens;
            private readonly int length;
            private readonly CancellationToken cancellationToken;

            public T Current { get; private set; }

            public UnBuffedAsyncEnumerator(DbDataReader reader, Span<int> span, RecordFactory<T> factory, ReaderState state, ref CancellationToken cancellationToken)
            {
                this.reader = reader;
                this.factory = factory;
                this.state = state;
                this.cancellationToken = cancellationToken;
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
                if (await reader.ReadAsync(cancellationToken))
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