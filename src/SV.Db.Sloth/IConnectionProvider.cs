using SV.Db.Sloth.Statements;
using SV.Db.Sloth;
using System.Data.Common;

namespace SV.Db
{
    public interface IConnectionProvider
    {
        DbConnection Create(string connectionString);

        PageResult<T> ExecuteQuery<T>(string connectionString, DbEntityInfo info, SelectStatement statement);

        Task<PageResult<T>> ExecuteQueryAsync<T>(string connectionString, DbEntityInfo info, SelectStatement statement, CancellationToken cancellationToken = default);

        Task<int> ExecuteInsertAsync<T>(DbConnection dbConnection, DbEntityInfo info, T data, CancellationToken cancellationToken);

        Task<int> ExecuteInsertAsync<T>(DbConnection dbConnection, DbEntityInfo info, IEnumerable<T> data, int batchSize, CancellationToken cancellationToken);
    }
}