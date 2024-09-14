using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using System;
using System.Collections;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SV.Db.Analyzers
{
    public class GeneratedMapping
    {
        public List<SourceState> Sources { get; private set; } = new List<SourceState>();

        public string Code { get; set; }
        public string ClassName { get; set; }
    }

    public static class GenerateMappingHandler
    {
        internal static readonly FrozenDictionary<SpecialType, (string dbType, string readerMethod)> DbTypeMapping = new Dictionary<SpecialType, (string dbType, string readerMethod)>()
        {
            { SpecialType.System_Boolean, ("DbType.Boolean","GetBoolean")},
            {SpecialType.System_Char,("DbType.String","GetChar") },
            {SpecialType.System_SByte,("DbType.SByte","#") },
            {SpecialType.System_Byte,("DbType.Byte","GetByte") },
            {SpecialType.System_Int16,("DbType.Int16","GetInt16") },
            {SpecialType.System_Int32,("DbType.Int32","GetInt32") },
            {SpecialType.System_Int64,("DbType.Int64","GetInt64") },
            {SpecialType.System_UInt16,("DbType.UInt16","#") },
            {SpecialType.System_UInt32,("DbType.UInt32","#") },
            {SpecialType.System_UInt64,("DbType.UInt64","#") },
            {SpecialType.System_Decimal,("DbType.Decimal","GetDecimal") },
            {SpecialType.System_Single,("DbType.Single","GetFloat") },
            {SpecialType.System_Double,("DbType.Double","GetDouble") },
            {SpecialType.System_String,("DbType.String","GetString") },
            {SpecialType.System_DateTime,("DbType.DateTime","GetDateTime") },
        }.ToFrozenDictionary();

        public static void GenerateMapping(this SourceState source, Dictionary<string, GeneratedMapping> map)
        {
            if (source.Args == null && source.ReturnType == null) return;

            if (source.NeedGenerateArgs())
            {
                source.GeneratedArgs = GenerateMapping(source.Args.Type, map, source);
            }

            if (source.NeedGenerateReturnType())
            {
                source.GeneratedReturn = GenerateMapping(source.ReturnType, map, source);
            }
        }

        private static void Add(Dictionary<string, List<SourceState>> dict, string key, SourceState source)
        {
            if (!dict.TryGetValue(key, out var value))
            {
                value = new List<SourceState>();
                dict.Add(key, value);
            }
            value.Add(source);
        }

        private static string GenerateMapping(ITypeSymbol type, Dictionary<string, GeneratedMapping> dict, SourceState source)
        {
            var key = type.ToFullName();
            if (!dict.TryGetValue(key, out var r))
            {
                r = GenerateMappingCode(type, key);
                dict.Add(key, r);
            }
            r.Sources.Add(source);
            return key;
        }

        private static GeneratedMapping GenerateMappingCode(ITypeSymbol type, string typeName)
        {
            var r = new GeneratedMapping();
            if (type.IsAnonymousType)
            {
                r.ClassName = $"Anonymous_{Guid.NewGuid():N}";
                // todo
            }
            if (type.IsTupleType)
            {
                r.ClassName = $"Tuple_{Guid.NewGuid():N}";
                // todo
            }
            else
            {
                r.ClassName = $"{type.Name}_{Guid.NewGuid():N}";
                var (readTokens, read) = GenerateReadTokens(type);
                r.Code = @$"
public class {r.ClassName} : RecordFactory<{typeName}>
{{
    public override void SetParams(IDbCmd cmd, {typeName} args)
    {{
        var ps = cmd.Parameters;
        DbParameter p;
        {GenerateSetParams(type)}
    }}

    protected override void GenerateReadTokens(DbDataReader reader, Span<int> tokens)
    {{
        for (int i = 0; i < reader.FieldCount; i++)
        {{
            var name = reader.GetName(i);
            var type = reader.GetFieldType(i);
            switch (StringHashing.HashOrdinalIgnoreCase(name))
            {{
                {readTokens}
                default:
                    break;
            }}
        }}
    }}

    protected override {typeName} Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
    {{
        var d = {GenerateCtor(type, typeName)};
        for (int j = 0; j < tokens.Length; j++)
        {{
            switch (tokens[j])
            {{
                {read}
                default:
                    break;
            }}
        }}
        return d;
    }}
}}
";
            }
            return r;
        }

        private static string GenerateCtor(ITypeSymbol type, string typeName)
        {
            var ctor = type.ChooseConstructor();
            string p = string.Empty;
            if (ctor != null && ctor.Parameters.Length > 0)
            { 
                p = string.Join(",", ctor.Parameters.Select(i => "default"));
            }
            return $"new {typeName}({p})";
        }

        private static (string, string) GenerateReadTokens(ITypeSymbol type)
        {
            var i = 0;
            var tokens = new StringBuilder();
            var read = new StringBuilder();
            foreach (var item in type.GetAllSettableProperties())
            {
                var dbType = GetDbType(item);
                i = GenerateReadTokens(i, tokens, read, item.Type, item.Name, dbType, item.GetColumnAttribute());

            }
            foreach (var item in type.GetAllPublicFields())
            {
                var dbType = GetDbType(item);
                i = GenerateReadTokens(i, tokens, read, item.Type, item.Name, dbType, item.GetColumnAttribute());
            }
            return (tokens.ToString(), read.ToString());
        }

        private static int GenerateReadTokens(int i, StringBuilder tokens, StringBuilder read, ITypeSymbol iType, string name, (string dbType, string readerMethod) dbType, ColumnAttributeData columnAttributeData)
        {
            var colName = columnAttributeData?.GetName(name) ?? name;
            var customConvertFromDbMethod = columnAttributeData?.GetCustomConvertFromDbMethod();
            if (dbType.readerMethod == "#" || !string.IsNullOrWhiteSpace(customConvertFromDbMethod))
            {
                var x = ++i;
                var tt = iType.GetUnderlyingType().ToFullName();
                tokens.Append($@"
// {colName}
case {StringHashing.HashOrdinalIgnoreCase(colName)}: 
tokens[i] = {x}; break;");
                read.Append($@"
                    case {x}:
                        d.{name} = reader.IsDBNull(j) ? default : {(string.IsNullOrWhiteSpace(customConvertFromDbMethod) ? $"DBUtils.As<{tt}>" : customConvertFromDbMethod)}(reader.GetValue(j));
                        break;
");
            }
            else if (iType.IsEnum())
            {
                var x = ++i;
                var tt = (iType.IsNullable() ? iType.GetNullableUnderlyingType() : iType).ToFullName();
                tokens.Append($@"
// {colName}
case {StringHashing.HashOrdinalIgnoreCase(colName)}: 
tokens[i] = {x}; break;");
                read.Append($@"
                    case {x}:
                        d.{name} = reader.IsDBNull(j) ? default : DBUtils.ToEnum<{tt}>(reader.GetValue(j));
                        break;
");
            }
            else if (!string.IsNullOrWhiteSpace(dbType.dbType))
            {
                var x = ++i;
                var y = ++i;
                var tt = iType.GetUnderlyingType().ToFullName();
                tokens.Append($@"
// {colName}
case {StringHashing.HashOrdinalIgnoreCase(colName)}: 
tokens[i] = type == typeof({tt}) ? {x} : {y}; break;");
                read.Append($@"
                    case {x}:
                        d.{name} = reader.IsDBNull(j) ? default : reader.{dbType.readerMethod}(j);
                        break;
                    case {y}:
                        d.{name} = reader.IsDBNull(j) ? default : DBUtils.As<{tt}>(reader.GetValue(j));
                        break;
");
            }
            else
            {
                tokens.Append($@"// ingore {iType.ToFullName()}");
            }

            return i;
        }

        private static string GenerateSetParams(ITypeSymbol type)
        {
            return GenerateSetParamsProperties(type) + GenerateSetParamsFields(type);
        }

        private static string GenerateSetParamsProperties(ITypeSymbol type)
        {
            var sb = new StringBuilder();
            foreach (var item in type.GetAllGettableProperties())
            {
                var dbType = GetDbType(item);
                GenerateSetParam(sb, dbType.dbType, item.Name, item.GetColumnAttribute(), item.Type);
            }
            return sb.ToString();
        }

        private static void GenerateSetParam(StringBuilder sb, string dbType, string name, ColumnAttributeData columnAttributeData, ITypeSymbol symbol)
        {
            var customConvertToDbMethod = columnAttributeData?.GetCustomConvertToDbMethod();

            if (string.IsNullOrWhiteSpace(dbType) && (string.IsNullOrWhiteSpace(customConvertToDbMethod) || string.IsNullOrWhiteSpace(columnAttributeData?.Type))) return;
            sb.Append($@"
p = cmd.CreateParameter();
p.Direction = {(string.IsNullOrWhiteSpace(columnAttributeData?.Direction) ? "ParameterDirection.Input" : columnAttributeData.Direction)};
p.ParameterName = {(string.IsNullOrWhiteSpace(columnAttributeData?.Name) ? $"\"{name}\"" : columnAttributeData.Name)};
p.DbType = {(string.IsNullOrWhiteSpace(columnAttributeData?.Type) ? dbType : columnAttributeData.Type)};
p.Value = {(string.IsNullOrWhiteSpace(customConvertToDbMethod) ? $"args.{name}{(symbol.IsNullable() ? ".GetValueOrDefault()": "")}" : $"{customConvertToDbMethod}(args.{name})")};
{(string.IsNullOrWhiteSpace(columnAttributeData?.Precision) ? "" : $"p.Precision = {columnAttributeData.Precision};" )}
{(string.IsNullOrWhiteSpace(columnAttributeData?.Scale) ? "" : $"p.Scale = {columnAttributeData.Scale};" )}
{(string.IsNullOrWhiteSpace(columnAttributeData?.Size) ? "" : $"p.Size = {columnAttributeData.Size};")}
ps.Add(p);
");
        }

        private static string GenerateSetParamsFields(ITypeSymbol type)
        {
            var sb = new StringBuilder();
            foreach (var item in type.GetAllPublicFields())
            {
                var dbType = GetDbType(item);
                GenerateSetParam(sb, dbType.dbType, item.Name, item.GetColumnAttribute(), item.Type);
            }
            return sb.ToString();
        }

        private static (string dbType, string readerMethod) GetDbType(IFieldSymbol item)
        {
            return GetDbType(item.Type);
        }

        private static (string dbType, string readerMethod) GetDbType(IPropertySymbol item)
        {
            return GetDbType(item.Type);
        }

        private static (string dbType, string readerMethod) GetDbType(ITypeSymbol type)
        {
            var t = type.GetUnderlyingType();
            if (DbTypeMapping.TryGetValue(t.SpecialType, out var v))
                return v;
            if (t.ToFullName() == "global::System.Guid")
                return ("DbType.Guid", "GetGuid");
            return (null, null);
        }

        public static string GenerateCode(this Dictionary<string, GeneratedMapping> map)
        {
            var src = $@"
// <auto-generated/>
#pragma warning disable 8019 //disable 'unnecessary using directive' warning
using System;
using System.Data;
using System.Data.Common;
using System.Runtime.CompilerServices;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using SV.Db;

namespace SV.Db
{{
    {string.Join("", map.Select(i => i.Value.Code))}

    public static partial class SVDbInitializer
    {{
        [ModuleInitializer]
        internal static void InitFunc()
        {{
            {string.Join("", map.Select(i => $"RecordFactory.RegisterRecordFactory<{i.Key}>(new {i.Value.ClassName}());"))}
        }}
    }}
}}
            ";
            return CSharpSyntaxTree.ParseText(SourceText.From(src)).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
        }
    }
}