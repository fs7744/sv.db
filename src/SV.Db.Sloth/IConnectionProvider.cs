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
    }
}