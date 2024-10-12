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
        public string SelectAll { get; set; }

        internal static (string Name, string Field)? ConvertSelectMember(MemberInfo info, FrozenDictionary<string, ColumnAttribute> columns)
        {
            var select = info.GetCustomAttribute<SelectAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? (columns.TryGetValue(info.Name, out var c) ? c.Name : info.Name) : select.Field);
        }

        internal static (string Name, string Field)? ConvertWhereMember(MemberInfo info, FrozenDictionary<string, ColumnAttribute> columns)
        {
            var select = info.GetCustomAttribute<WhereAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? (columns.TryGetValue(info.Name, out var c) ? c.Name : info.Name) : select.Field);
        }

        internal static (string Name, string Field)? ConvertOrderByMember(MemberInfo info, FrozenDictionary<string, ColumnAttribute> columns)
        {
            var select = info.GetCustomAttribute<OrderByAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? (columns.TryGetValue(info.Name, out var c) ? c.Name : info.Name) : select.Field);
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
                var fields = t.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(i => i.MemberType == MemberTypes.Property || i.MemberType == MemberTypes.Field).ToList();
                c.Columns = fields.Select(i => (i.Name, i.GetCustomAttribute<ColumnAttribute>()))
                    .Where(i => i.Item2 != null)
                    .ToFrozenDictionary(i => i.Name, i =>
                    {
                        if (string.IsNullOrWhiteSpace(i.Item2.Name))
                        {
                            i.Item2.Name = i.Name;
                        }
                        return i.Item2;
                    }, StringComparer.OrdinalIgnoreCase);
                c.SelectFields = fields.Select(i => DbEntityInfo.ConvertSelectMember(i, c.Columns))
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                var all = c.SelectFields.Where(i => i.Key.Equals("*")).Select(i => i.Value).FirstOrDefault(i => !string.IsNullOrWhiteSpace(i));
                c.SelectAll = all != null ? all : string.Join(",", c.SelectFields.Select(i => i.Value));
                c.WhereFields = fields.Select(i => DbEntityInfo.ConvertWhereMember(i, c.Columns))
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                c.OrderByFields = fields.Select(i => DbEntityInfo.ConvertOrderByMember(i, c.Columns))
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);

                Cache = c;
            }

            return c;
        }
    }
}