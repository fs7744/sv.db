using SV.Db.Sloth.Attributes;
using System.Collections.Frozen;
using System.Reflection;

namespace SV.Db
{
    public class DbEntityInfo
    {
        public string? DbKey { get; set; }

        public string Table { get; set; }
        public FrozenDictionary<string, string> SelectFields { get; set; }
        public FrozenDictionary<string, string> WhereFields { get; set; }
        public FrozenDictionary<string, string> OrderByFields { get; set; }

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
    }

    public static class DbEntityInfo<T>
    {
        public static DbEntityInfo Cache;
    }
}