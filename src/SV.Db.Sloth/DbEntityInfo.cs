using SV.Db.Sloth.Attributes;
using System.Collections.Frozen;
using System.Reflection;

namespace SV.Db
{
    public class DbEntityInfo
    {
        public int Timeout { get; set; } = 30;
        public string? DbKey { get; set; }

        public string Table { get; set; }
        public FrozenDictionary<string, string> SelectFields { get; set; }
        public FrozenDictionary<string, string> WhereFields { get; set; }
        public FrozenDictionary<string, string> OrderByFields { get; set; }
        public FrozenDictionary<string, ColumnAttribute> Columns { get; set; }

        internal static (string Name, string Field)? ConvertSelectMember(MemberInfo info)
        {
            var select = info.GetCustomAttribute<SelectAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? info.Name : select.Field);
        }

        internal static (string Name, string Field)? ConvertWhereMember(MemberInfo info)
        {
            var select = info.GetCustomAttribute<WhereAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? info.Name : select.Field);
        }

        internal static (string Name, string Field)? ConvertOrderByMember(MemberInfo info)
        {
            var select = info.GetCustomAttribute<OrderByAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? info.Name : select.Field);
        }

        internal static ColumnAttribute ConvertColumnMember(MemberInfo info)
        {
            var r = info.GetCustomAttribute<ColumnAttribute>();
            if (r != null)
            {
                r.Name = info.Name;
            }
            return r;
        }
    }

    public static class DbEntityInfo<T>
    {
        public static DbEntityInfo Cache;

        public static DbEntityInfo Get()
        {
            var c = Cache;
            if (c == null)
            {
                c = new DbEntityInfo();
                var t = typeof(T);
                var d = t.GetCustomAttribute<DbAttribute>();
                c.DbKey = d?.Key;
                if (string.IsNullOrWhiteSpace(c.DbKey))
                    throw new KeyNotFoundException("DbAttribute");
                c.Timeout = d.Timeout;
                c.Table = t.GetCustomAttribute<TableAttribute>()?.Table;
                //if (string.IsNullOrWhiteSpace(c.Table))
                //    throw new KeyNotFoundException("TableAttribute");
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
                c.Columns = fields.Select(DbEntityInfo.ConvertColumnMember)
                    .Where(i => i != null)
                    .ToFrozenDictionary(i => i.Name, i => i, StringComparer.OrdinalIgnoreCase);
                Cache = c;
            }

            return c;
        }
    }
}