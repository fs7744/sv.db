using System.Collections.Concurrent;
using System.Data.Common;
using System.Transactions;

namespace SV.Db
{
    public interface ITransactionConnectionFactory
    {
        public DbConnection Create(string db, string connectionString);
    }

    public class TransactionConnection : DbConnection
    {
        public DbConnection RealConnection { get; }

        public TransactionConnection(DbConnection connection)
        {
            this.RealConnection = connection;
        }

#pragma warning disable CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).
        public override string ConnectionString { get => RealConnection.ConnectionString; set => RealConnection.ConnectionString = value; }
#pragma warning restore CS8765 // Nullability of type of parameter doesn't match overridden member (possibly because of nullability attributes).

        public override string Database => RealConnection.Database;

        public override string DataSource => RealConnection.DataSource;

        public override string ServerVersion => RealConnection.ServerVersion;

        public override System.Data.ConnectionState State => RealConnection.State;

        public override void ChangeDatabase(string databaseName)
        {
            RealConnection.ChangeDatabase(databaseName);
        }

        public override Task ChangeDatabaseAsync(string databaseName, CancellationToken cancellationToken = default)
        {
            return RealConnection.ChangeDatabaseAsync(databaseName, cancellationToken);
        }

        public override void EnlistTransaction(Transaction? transaction)
        {
        }

        public override void Close()
        {
        }

        public override Task CloseAsync()
        {
            return Task.CompletedTask;
        }

        public override void Open()
        {
            if (RealConnection.State != System.Data.ConnectionState.Open)
            {
                RealConnection.Open();
            }
        }

        public override Task OpenAsync(CancellationToken cancellationToken)
        {
            if (RealConnection.State != System.Data.ConnectionState.Open)
            {
                return RealConnection.OpenAsync(cancellationToken);
            }
            return Task.CompletedTask;
        }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return RealConnection.BeginTransaction(isolationLevel);
        }

        protected override ValueTask<DbTransaction> BeginDbTransactionAsync(System.Data.IsolationLevel isolationLevel, CancellationToken cancellationToken)
        {
            return RealConnection.BeginTransactionAsync(isolationLevel, cancellationToken);
        }

        protected override DbCommand CreateDbCommand()
        {
            return RealConnection.CreateCommand();
        }
    }

    public static class TransactionConnectionFactory
    {
        private static readonly ConcurrentDictionary<Transaction, ConcurrentDictionary<string, TransactionConnection>> connections
            = new();

        public static DbConnection GetOrAdd(string connectionString, Func<string, DbConnection> create)
        {
            var currentTransaction = Transaction.Current;
            if (currentTransaction == null)
            {
                return create(connectionString);
            }
            var dict = connections.GetOrAdd(currentTransaction, NewConnectionsDict);
            return dict.GetOrAdd(connectionString, key => new TransactionConnection(create(key)));
        }

        private static ConcurrentDictionary<string, TransactionConnection> NewConnectionsDict(Transaction transaction)
        {
            transaction.TransactionCompleted += TransactionCompleted;
            return new ConcurrentDictionary<string, TransactionConnection>();
        }

        private static void TransactionCompleted(object? sender, TransactionEventArgs e)
        {
            if (e.Transaction != null && connections.TryGetValue(e.Transaction, out var dict))
            {
                foreach (var conn in dict.Values)
                {
                    conn.RealConnection.Close();
                    conn.RealConnection.Dispose();
                    conn.Dispose();
                }

                connections.TryRemove(e.Transaction, out dict);
            }
        }
    }
}