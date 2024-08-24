using System.Collections.Concurrent;
using System.Data.Common;
using System.Transactions;

namespace SV.Db
{
    public class TransactionConnection : DbConnection
    {
        public DbConnection RealConnection { get; }

        public TransactionConnection(DbConnection connection)
        {
            this.RealConnection = connection;
        }

        public override string ConnectionString { get => RealConnection.ConnectionString; set => RealConnection.ConnectionString = value; }

        public override string Database => RealConnection.Database;

        public override string DataSource => RealConnection.DataSource;

        public override string ServerVersion => RealConnection.ServerVersion;

        public override System.Data.ConnectionState State => RealConnection.State;

        public override void ChangeDatabase(string databaseName)
        {
            RealConnection.ChangeDatabase(databaseName);
        }

        public override void Close()
        {
        }

        public override void Open()
        {
            RealConnection.Open();
        }

        protected override DbTransaction BeginDbTransaction(System.Data.IsolationLevel isolationLevel)
        {
            return RealConnection.BeginTransaction(isolationLevel);
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

        private static void TransactionCompleted(object sender, TransactionEventArgs e)
        {
            if (connections.TryGetValue(e.Transaction, out ConcurrentDictionary<string, TransactionConnection> dict))
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