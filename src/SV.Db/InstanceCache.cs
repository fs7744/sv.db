namespace SV.Db
{
    public static class InstanceCache<T> where T : new()
    {
        public static readonly T Instance = new();
    }
}