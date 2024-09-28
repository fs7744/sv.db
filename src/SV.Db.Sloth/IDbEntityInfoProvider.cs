namespace SV.Db
{
    public interface IDbEntityInfoProvider
    {
        DbEntityInfo GetDbEntityInfo(string key);
    }
}