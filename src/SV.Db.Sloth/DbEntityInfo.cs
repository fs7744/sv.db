using System.Collections.Frozen;
using System.Reflection;

namespace SV.Db
{
    public class DbEntityInfo
    {
        public string? DbKey { get; set; }
        public FrozenDictionary<string, DbFieldInfo> Fields { get; set; }

        internal static DbFieldInfo ConvertMember(MemberInfo info)
        {
            var r = new DbFieldInfo() { Name = info.Name };

            return r;
        }
    }

    public class DbFieldInfo
    {
        public string Name { get; set; }
    }

    public static class DbEntityInfo<T>
    {
        public static DbEntityInfo Cache;
    }
}