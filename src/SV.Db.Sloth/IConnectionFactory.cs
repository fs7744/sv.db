using SV.Db.Sloth;
using SV.Db.Sloth.Statements;
using System.Data.Common;

namespace SV.Db
{
    public interface IConnectionFactory
    {
        DbConnection GetConnection(string key);

        PageResult<T> ExecuteQuery<T>(DbEntityInfo info, SelectStatement statement);

        Task<PageResult<T>> ExecuteQueryAsync<T>(DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default);

        public DbEntityInfo GetDbEntityInfoOfT<T>();

        public DbEntityInfo GetDbEntityInfo(string key);

        public SelectStatementBuilder<T> From<T>();

        public SelectStatementBuilder From(string key);

        (string dbType, string connectionString) Get(string key);
    }
}