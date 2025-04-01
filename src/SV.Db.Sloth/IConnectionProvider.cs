using SV.Db.Sloth.Statements;
using SV.Db.Sloth;
using System.Data.Common;

namespace SV.Db
{
    public interface IConnectionProvider
    {
        PageResult<T> ExecuteQuery<T>(string connectionString, DbEntityInfo info, SelectStatement statement);

        Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default);

        Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken);

        Task<R> ExecuteInsertRowAsync<T, R>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken);

        Task<int> ExecuteInsertAsync<T>(string connectionString, DbEntityInfo info, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken);

        Task<int> ExecuteUpdateAsync<T>(string connectionString, DbEntityInfo info, T data, CancellationToken cancellationToken);

        void Init(IServiceProvider provider);
    }

    public interface IDbConnectionProvider : IConnectionProvider
    {
        DbConnection Create(string connectionString);
    }
}