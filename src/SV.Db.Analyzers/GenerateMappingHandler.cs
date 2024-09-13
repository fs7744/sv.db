using Microsoft.CodeAnalysis;
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
        internal static readonly FrozenDictionary<SpecialType, string> DbTypeMapping = new Dictionary<SpecialType, string>()
        {
            { SpecialType.System_Boolean, "DbType.Boolean"},
            {SpecialType.System_Char,"DbType.String" },
            {SpecialType.System_SByte,"DbType.SByte" },
            {SpecialType.System_Byte,"DbType.Byte" },
            {SpecialType.System_Int16,"DbType.Int16" },
            {SpecialType.System_Int32,"DbType.Int32" },
            {SpecialType.System_Int64,"DbType.Int64" },
            {SpecialType.System_UInt16,"DbType.UInt16" },
            {SpecialType.System_UInt32,"DbType.UInt32" },
            {SpecialType.System_UInt64,"DbType.UInt64" },
            {SpecialType.System_Decimal,"DbType.Decimal" },
            {SpecialType.System_Single,"DbType.Single" },
            {SpecialType.System_Double,"DbType.Double" },
            {SpecialType.System_String,"DbType.String" },
            {SpecialType.System_DateTime,"DbType.DateTime" },
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
            var key = type.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
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
            var r = new GeneratedMapping() { ClassName = $"{type.Name}_{Guid.NewGuid().ToString("N")}" };
            if (type.IsAnonymousType)
            {
                // todo
            }
            if (type.IsTupleType)
            {
                // todo
            }
            else
            {
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
                default:
                    break;
            }}
        }}
    }}

    protected override {typeName}? Read(DbDataReader reader, ref ReadOnlySpan<int> tokens)
    {{
        var d = new {typeName}();
        for (int j = 0; j < tokens.Length; j++)
        {{
            switch (tokens[j])
            {{
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
                if (string.IsNullOrEmpty(dbType)) continue;
                sb.Append($@"
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = ""{item.Name}"";
            p.DbType = {dbType};
            p.Value = args.{item.Name};
            ps.Add(p);
");
            }
            return sb.ToString();
        }

        private static string GenerateSetParamsFields(ITypeSymbol type)
        {
            var sb = new StringBuilder();
            foreach (var item in type.GetAllPublicFields())
            {
                var dbType = GetDbType(item);
                if (string.IsNullOrEmpty(dbType)) continue;
                sb.Append($@"
            p = cmd.CreateParameter();
            p.Direction = ParameterDirection.Input;
            p.ParameterName = ""{item.Name}"";
            p.DbType = {dbType};
            p.Value = args.{item.Name};
            ps.Add(p);
");
            }
            return sb.ToString();
        }

        private static string GetDbType(IFieldSymbol item)
        {
            if (DbTypeMapping.TryGetValue(item.Type.SpecialType, out var v))
                return v;
            return null;
        }

        private static string GetDbType(IPropertySymbol item)
        {
            if (DbTypeMapping.TryGetValue(item.Type.SpecialType, out var v))
                return v;
            return null;
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

            return src;
        }
    }
}