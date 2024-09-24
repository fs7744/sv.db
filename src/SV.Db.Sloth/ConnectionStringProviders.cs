using SV.Db.Sloth.Attributes;
using System.Collections.Frozen;
using System.Reflection;

namespace SV.Db
{
    public class ConnectionStringProviders : ConnectionStringProvider, IConnectionFactory
    {
        private readonly IConnectionStringProvider[] providers;

        public ConnectionStringProviders(IConnectionStringProvider[] providers)
        {
            this.providers = providers;
        }

        public override bool ContainsKey(string key)
        {
            foreach (var item in providers)
            {
                if (item.ContainsKey(key))
                    return true;
            }
            return false;
        }

        public override (string dbType, string connectionString) Get(string key)
        {
            foreach (var item in providers)
            {
                if (item.ContainsKey(key))
                    return item.Get(key);
            }
            throw new KeyNotFoundException(key);
        }

        public DbEntityInfo GetDbEntityInfo<T>()
        {
            var c = DbEntityInfo<T>.Cache;
            if (c == null)
            {
                c = new DbEntityInfo();
                var t = typeof(T);
                c.DbKey = t.GetCustomAttribute<DbAttribute>()?.Key;
                if (string.IsNullOrWhiteSpace(c.DbKey))
                    throw new KeyNotFoundException("DbAttribute");
                c.Table = t.GetCustomAttribute<TableAttribute>()?.Table;
                if (string.IsNullOrWhiteSpace(c.Table))
                    throw new KeyNotFoundException("TableAttribute");
                var fields = t.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(i => i.MemberType == MemberTypes.Property || i.MemberType == MemberTypes.Field).ToList();
                c.SelectFields = fields.Select(DbEntityInfo.ConvertSelectMember)
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                c.WhereFields = fields.Select(DbEntityInfo.ConvertWhereMember)
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                c.OrderByFields = fields.Select(DbEntityInfo.ConvertOrderByMember)
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                DbEntityInfo<T>.Cache = c;
            }

            return c;
        }
    }
}