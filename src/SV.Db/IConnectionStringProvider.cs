namespace SV.Db
{
    public interface IConnectionStringProvider
    {
        public (string dbType, string connectionString) Get(string key);

        public bool ContainsKey(string key);
    }
}