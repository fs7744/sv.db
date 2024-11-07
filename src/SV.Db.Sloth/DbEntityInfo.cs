using SV.Db.Sloth.Attributes;
using System.Collections.Frozen;
using System.Data;
using System.Linq.Expressions;
using System.Reflection;

namespace SV.Db
{
    public class DbEntityInfo
    {
        public int Timeout { get; set; } = 30;
        public string? DbKey { get; set; }
        public string Table { get; set; }
        public string UpdateTable { get; set; }
        public FrozenDictionary<string, string> SelectFields { get; set; }
        public FrozenDictionary<string, string> WhereFields { get; set; }
        public FrozenDictionary<string, string> OrderByFields { get; set; }
        public FrozenDictionary<string, ColumnAttribute> Columns { get; set; }
        public FrozenDictionary<string, UpdateAttribute> UpdateColumns { get; set; }

        internal static (string Name, string Field)? ConvertSelectMember(MemberInfo info)
        {
            var select = info.GetCustomAttribute<SelectAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? info.Name : select.Field);
        }

        internal static (string Name, string Field)? ConvertWhereMember(MemberInfo info, FrozenDictionary<string, string> selectFields)
        {
            var select = info.GetCustomAttribute<WhereAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? (selectFields.TryGetValue(info.Name, out var c) ? c : info.Name) : select.Field);
        }

        internal static (string Name, string Field)? ConvertOrderByMember(MemberInfo info, FrozenDictionary<string, string> selectFields)
        {
            var select = info.GetCustomAttribute<OrderByAttribute>();
            if (select == null || select.NotAllow)
            {
                return null;
            }
            return (info.Name, string.IsNullOrWhiteSpace(select.Field) ? (selectFields.TryGetValue(info.Name, out var c) ? c : info.Name) : select.Field);
        }

        internal static readonly FrozenDictionary<TypeCode, DbType> DbTypeMapping = new Dictionary<TypeCode, DbType>()
        {
            {TypeCode.Boolean, DbType.Boolean},
            {TypeCode.Char,DbType.String },
            {TypeCode.SByte,DbType.SByte },
            {TypeCode.Byte,DbType.Byte },
            {TypeCode.Int16,DbType.Int16 },
            {TypeCode.Int32,DbType.Int32 },
            {TypeCode.Int64,DbType.Int64 },
            {TypeCode.UInt16,DbType.UInt16 },
            {TypeCode.UInt32,DbType.UInt32 },
            {TypeCode.UInt64,DbType.UInt64 },
            {TypeCode.Decimal,DbType.Decimal },
            {TypeCode.Single,DbType.Single },
            {TypeCode.Double,DbType.Double },
            {TypeCode.String,DbType.String },
            {TypeCode.DateTime,DbType.DateTime },
        }.ToFrozenDictionary();

        private string selectAll;

        public string SelectAll(Func<string, string, string> func, string separator)
        {
            if (selectAll == null)
            {
                selectAll = string.Join(separator, SelectFields.Select(i => func(i.Key, i.Value)));
            }

            return selectAll;
        }

        private FrozenSet<string> jsonFields;

        public FrozenSet<string> GetJsonFields()
        {
            if (jsonFields == null)
            {
                jsonFields = Columns.Where(i => i.Value.IsJson).Select(i => i.Key).Distinct(StringComparer.OrdinalIgnoreCase).ToFrozenSet(StringComparer.OrdinalIgnoreCase);
            }

            return jsonFields;
        }

        private string insertSql;

        public string GetInsertSql(Func<DbEntityInfo, string> func)
        {
            if (insertSql == null)
            {
                insertSql = func(this);
            }

            return insertSql;
        }

        public Func<object, IEnumerable<KeyValuePair<string, string>>> GetUpdateFields { get; set; }
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
                var table = t.GetCustomAttribute<TableAttribute>();
                if (table != null)
                {
                    c.Table = table.Table;
                    c.UpdateTable = string.IsNullOrWhiteSpace(table.UpdateTable) ? table.Table : table.UpdateTable;
                }

                var fields = t.GetMembers(BindingFlags.Public | BindingFlags.Instance).Where(i => i.MemberType == MemberTypes.Property || i.MemberType == MemberTypes.Field).ToList();
                c.Columns = fields.Select(i => (i.Name, i.GetCustomAttribute<ColumnAttribute>() ?? (i.GetCustomAttribute<NotColumnAttribute>() == null ? new ColumnAttribute()
                {
                    Name = i.Name,
                    Type = DbEntityInfo.DbTypeMapping.TryGetValue(Type.GetTypeCode(i.GetType()), out var d) ? d : DbType.String
                } : null)))
                    .Where(i => i.Item2 != null)
                    .ToFrozenDictionary(i => i.Name, i =>
                    {
                        if (string.IsNullOrWhiteSpace(i.Item2.Name))
                        {
                            i.Item2.Name = i.Name;
                        }
                        return i.Item2;
                    }, StringComparer.OrdinalIgnoreCase);
                c.UpdateColumns = fields.Select(i => (i.Name, i.GetCustomAttribute<UpdateAttribute>()))
                    .Where(i =>
                    {
                        if (i.Item2 == null) return false;
                        if (!string.IsNullOrWhiteSpace(i.Item2.Field)) return true;
                        if (c.Columns.TryGetValue(i.Name, out var cc))
                        {
                            i.Item2.Field = cc.Name;
                        }
                        if (string.IsNullOrWhiteSpace(i.Item2.Field))
                            i.Item2.Field = i.Name;
                        return true;
                    })
                    .ToFrozenDictionary(i => i.Name, i => i.Item2, StringComparer.OrdinalIgnoreCase);
                c.SelectFields = fields.Select(DbEntityInfo.ConvertSelectMember)
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                c.WhereFields = fields.Select(i => DbEntityInfo.ConvertWhereMember(i, c.SelectFields))
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);
                c.OrderByFields = fields.Select(i => DbEntityInfo.ConvertOrderByMember(i, c.SelectFields))
                    .Where(i => i.HasValue)
                    .Select(i => i.Value)
                    .ToFrozenDictionary(i => i.Name, i => i.Field, StringComparer.OrdinalIgnoreCase);

                var ups = c.UpdateColumns.Where(i => !i.Value.PrimaryKey).ToArray();
                var structFields = ups.Where(i =>
                {
                    var f = fields.First(x => x.Name.Equals(i.Key, StringComparison.OrdinalIgnoreCase));
                    return f.DeclaringType.IsValueType && Nullable.GetUnderlyingType(f.DeclaringType) is null;
                }).Select(i => new KeyValuePair<string, string>(i.Key, i.Value.Field)).ToArray();
                var checkUps = ups.Select(i =>
                {
                    Type t;
                    var f = fields.First(x => x.Name.Equals(i.Key, StringComparison.OrdinalIgnoreCase));
                    if (f is PropertyInfo p)
                    {
                        t = p.PropertyType;
                    }
                    else if (f is FieldInfo field)
                    {
                        t = field.FieldType;
                    }
                    else
                    {
                        return null;
                    }
                    if (t.IsValueType && Nullable.GetUnderlyingType(t) is null)
                    {
                        return null;
                    }

                    var o = Expression.Parameter(typeof(object), "o");
                    Expression fg;

                    if (f.MemberType == MemberTypes.Property)
                    {
                        fg = Expression.Property(Expression.Convert(o, f.DeclaringType), f as PropertyInfo);
                    }
                    else
                    {
                        fg = Expression.Field(Expression.Convert(o, f.DeclaringType), f as FieldInfo);
                    }
                    var check = Expression.Lambda<Func<object, bool>>(Expression.Block(new Expression[] { Expression.Equal(fg, Expression.Constant(null)) }), o).Compile();
                    var rr = new KeyValuePair<string, string>(i.Key, i.Value.Field);
                    Func<object, KeyValuePair<string, string>?> r = o => check(o) ? null : rr;
                    return r;
                }).Where(i => i != null).ToArray();
                c.GetUpdateFields = o => structFields.Union(checkUps.Select(x => x(o)).Where(x => x.HasValue).Select(x => x.Value));
                Cache = c;
            }

            return c;
        }
    }
}