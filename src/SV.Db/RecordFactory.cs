﻿using System.Collections;
using System.Data;
using System.Runtime.InteropServices;

namespace SV.Db
{
    public abstract class RecordFactory<T>
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

        public virtual List<T> ReadBuffed(IDataReader reader)
        {
            List<T> results = [];
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
            var s = reader.FieldCount <= 64 ? MemoryMarshal.CreateSpan(ref MemoryMarshal.GetReference(stackalloc int[reader.FieldCount]), reader.FieldCount) : state.GetTokens();
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
    }
}