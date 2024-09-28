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

        public DbEntityInfo GetDbEntityInfo<T>();

        public SelectStatementBuilder<T> From<T>();

        public SelectStatementBuilder From(string key);
    }
}