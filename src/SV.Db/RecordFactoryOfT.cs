using System.Collections;
using System.Data;
using System.Data.Common;
using System.Runtime.InteropServices;
using System;

namespace SV.Db
{
    public interface IParamsSetter
    {
        void SetParams(DbCommand cmd, object args);

        void SetParams(DbBatchCommand cmd, object args);
    }

    public interface IParamsSetter<T> : IParamsSetter
    {
        void SetParams(DbCommand cmd, T args);

        void SetParams(DbBatchCommand cmd, T args);
    }

    public interface IRecordFactory<T>
    {
        T? Read(DbDataReader reader);

        List<T> ReadBuffed(DbDataReader reader, int estimateRow = 0);

        IEnumerable<T> ReadUnBuffed(DbDataReader reader);

        IAsyncEnumerable<T> ReadUnBuffedAsync(DbDataReader reader, CancellationToken cancellationToken = default);
    }

    public interface IDbCmd
    {
        public string CommandText { get; set; }

        public CommandType CommandType { get; set; }

        public DbParameterCollection Parameters { get; }

        public DbParameter CreateParameter();
    }

    public readonly struct DbCmd : IDbCmd
    {
        private readonly DbCommand command;

        public DbCmd(DbCommand command)
        {
            this.command = command;
        }

        public string CommandText { get => command.CommandText; set => command.CommandText = value; }
        public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

        public DbParameterCollection Parameters => command.Parameters;

        public DbParameter CreateParameter()
        {
            return command.CreateParameter();
        }
    }

    public readonly struct DbBatchCmd : IDbCmd
    {
        private readonly DbBatchCommand command;

        public DbBatchCmd(DbBatchCommand command)
        {
            this.command = command;
        }

        public string CommandText { get => command.CommandText; set => command.CommandText = value; }
        public CommandType CommandType { get => command.CommandType; set => command.CommandType = value; }

        public DbParameterCollection Parameters => command.Parameters;

        public DbParameter CreateParameter()
        {
            return command.CreateParameter();
        }
    }

    public abstract class RecordFactory<T> : IRecordFactory<T>, IParamsSetter<T>
    {
        public virtual void SetParams(DbCommand cmd, object args)
        {
            SetParams(cmd, (T)args);
        }

        public void SetParams(DbCommand cmd, T args)
        {
            SetParams(new DbCmd(cmd), args);
        }

        public virtual void SetParams(DbBatchCommand cmd, object args)
        {
            SetParams(cmd, (T)args);
        }

        public void SetParams(DbBatchCommand cmd, T args)
        {
            SetParams(new DbBatchCmd(cmd), args);
        }

        public abstract void SetParams(IDbCmd cmd, T args);

        protected abstract void GenerateReadTokens(DbDataReader reader, Span<int> tokens);

        protected abstract T? Read(DbDataReader reader, ref ReadOnlySpan<int> tokens);

        public virtual T? Read(DbDataReader reader)
        {
            var state = new ReaderState
            {
                Reader = reader
            };
            var s = state.GetTokens().AsSpan(state.FieldCount);
            GenerateReadTokens(reader, s);
            ReadOnlySpan<int> readOnlyTokens = s;
            return Read(reader, ref readOnlyTokens);
        }

        public virtual List<T> ReadBuffed(DbDataReader reader, int estimateRow = 0)
        {
            List<T?> results = new(estimateRow);
            if (reader.Read())
            {
                var state = new ReaderState
                {
                    Reader = reader
                };
                var s = state.GetTokens().AsSpan(state.FieldCount);
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
            return new UnBuffedEnumerator(reader, this, state);
        }

        internal unsafe struct UnBuffedEnumerator : IEnumerable<T?>, IEnumerator<T?>
        {
            private readonly DbDataReader reader;
            private readonly RecordFactory<T> factory;
            private readonly ReaderState state;

            public T? Current { get; private set; }

            object? IEnumerator.Current => Current;

            public UnBuffedEnumerator(DbDataReader reader, RecordFactory<T> factory, ReaderState state)
            {
                this.reader = reader;
                this.factory = factory;
                this.state = state;
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
                    var s = state.GetTokens().AsSpan(state.FieldCount);
                    ReadOnlySpan<int> readOnlyTokens = s;
                    Current = factory.Read(reader, ref readOnlyTokens);
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
            return new UnBuffedAsyncEnumerator(reader, this, state, ref cancellationToken);
        }

        internal class UnBuffedAsyncEnumerator : IAsyncEnumerable<T?>, IAsyncEnumerator<T?>
        {
            private readonly DbDataReader reader;
            private readonly RecordFactory<T> factory;
            private readonly ReaderState state;
            private readonly CancellationToken cancellationToken;

            public T? Current { get; private set; }

            public UnBuffedAsyncEnumerator(DbDataReader reader, RecordFactory<T> factory, ReaderState state, ref CancellationToken cancellationToken)
            {
                this.reader = reader;
                this.factory = factory;
                this.state = state;
                this.cancellationToken = cancellationToken;
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
                var s = state.GetTokens().AsSpan(state.FieldCount);
                ReadOnlySpan<int> readOnlyTokens = s;
                Current = factory.Read(reader, ref readOnlyTokens);
            }
        }
    }
}