using System.Data.Common;

namespace SV.Db
{
    public interface IConnectionProvider
    {
        DbConnection Create(string connectionString);
    }
}