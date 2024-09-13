using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;
using System.Text;

namespace SV.Db.Analyzers
{
    public class GeneratedMapping
    {
        public List<SourceState> Sources { get; private set; } = new List<SourceState>();

        public string Code { get; set; }
    }

    public static class GenerateMappingHandler
    {
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
            var key = type.ToDisplayString();
            if (!dict.TryGetValue(key, out var r))
            {
                r = GenerateMappingCode(type);
                dict.Add(key, r);
            }
            r.Sources.Add(source);
            return key;
        }

        private static GeneratedMapping GenerateMappingCode(ITypeSymbol type)
        {
            var r = new GeneratedMapping();
            if (type.IsAnonymousType)
            {
                // todo
            }

            return r;
        }
    }
}