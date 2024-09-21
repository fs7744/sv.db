using SV.Db.Sloth;
using SV.Db.Sloth.Statements;
using System.Data.Common;

namespace SV.Db
{
    public interface IConnectionFactory
    {
        DbConnection GetConnection(string key);

        PageResult<T> ExecuteQuery<T>(string key, SelectStatement statement);

        Task<PageResult<T>> ExecuteQueryAsync<T>(string key, SelectStatement statement, CancellationToken cancellationToken = default);
    }
}