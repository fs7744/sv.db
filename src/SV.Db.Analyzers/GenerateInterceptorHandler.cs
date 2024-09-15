using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace SV.Db.Analyzers
{
    public static class GenerateInterceptorHandler
    {
        internal static string GenerateCode(Dictionary<string, GeneratedMapping> map, Compilation compilation)
        {
            var kvs = map.Where(i => i.Value.NeedInterceptor).ToArray();
            if(kvs.Length == 0 ) return string.Empty;

            return $@"
namespace SV.Db
{{
    file static class GeneratedInterceptors_{Guid.NewGuid():N}
    {{
        {string.Join("", kvs.SelectMany(i => i.Value.Sources.Select(j => (j, i))).GroupBy(i => (i.j.GeneratedArgs, i.j.GeneratedReturn, MethodName: i.j.Invocation.TargetMethod.ToDisplayString())).Select(i => GenerateCode(i, map, compilation)))}
    }}
}}


namespace System.Runtime.CompilerServices
{{
    // this type is needed by the compiler to implement interceptors - it doesn't need to
    // come from the runtime itself, though

    [global::System.Diagnostics.Conditional(""DEBUG"")] // not needed post-build, so: evaporate
    [global::System.AttributeUsage(global::System.AttributeTargets.Method, AllowMultiple = true)]
    sealed file class InterceptsLocationAttribute : global::System.Attribute
    {{
        public InterceptsLocationAttribute(string path, int lineNumber, int columnNumber)
        {{
            _ = path;
            _ = lineNumber;
            _ = columnNumber;
        }}
    }}
}}
";
        }

        private static string GenerateCode(IGrouping<(string GeneratedArgs, string GeneratedReturn, string MethodName), (SourceState state, KeyValuePair<string, GeneratedMapping> kv)> v, Dictionary<string, GeneratedMapping> map, Compilation compilation)
        {
            var sb = new StringBuilder();

            foreach (var item in v)
            {
                var state = item.state;
                var location = state.Invocation.GetMemberLocation();
                var loc = location.GetLineSpan();
                var start = loc.StartLinePosition;
                sb.AppendLine(@$"[global::System.Runtime.CompilerServices.InterceptsLocationAttribute({SyntaxFactory.LiteralExpression(SyntaxKind.StringLiteralExpression, SyntaxFactory.Literal(location.SourceTree.GetInterceptorFilePath(compilation)))},{start.Line + 1},{start.Character + 1})]");
            }
            var vv = v.First();
            var op = vv.state.Invocation;
            sb.Append("internal static ");
            sb.Append(op.TargetMethod.ReturnType.ToFullName());
            sb.Append(" ");
            sb.Append(op.TargetMethod.Name);
            sb.Append("_");
            sb.Append(Guid.NewGuid().ToString("N"));
            //if (op.TargetMethod.IsGenericMethod)
            //{
            //    sb.Append("<");
            //    sb.Append(string.Join(",", op.TargetMethod.TypeParameters.Select(i => i.Name)));
            //    sb.Append(">");
            //}
            sb.Append("(");
            if (op.TargetMethod.IsExtensionMethod)
            {
                sb.Append(" this ");
            }
            sb.Append(string.Join(",", op.TargetMethod.Parameters.Select(i => @$"{(i.Type.IsAnonymousType ? "dynamic" : i.Type.ToFullName())} {i.Name}")));
            sb.AppendLine(")");
            sb.AppendLine("{");

            switch (op.TargetMethod.Name)
            {
                case "SetParams":
                    GenerateSetParamsMethod(sb, vv.kv.Value);
                    break;

                case "ExecuteReader":
                case "ExecuteReaderAsync":
                    GenerateExecuteReaderMethod(sb, vv.kv.Value, op, vv.state.IsAsync);
                    break;
                case "QueryFirstOrDefault":
                case "QueryFirstOrDefaultAsync":
                    GenerateQueryFirstOrDefaultMethod(sb, vv.kv.Value, vv.state.IsAsync);
                    break;
                case "Query":
                case "QueryAsync":
                    GenerateQueryMethod(sb, vv.kv.Value, vv.state.IsAsync);
                    break;
                default:
                    break;
            }
            sb.AppendLine("}");
            return sb.ToString();
        }

        private static void GenerateQueryFirstOrDefaultMethod(StringBuilder sb, GeneratedMapping value, bool isAsync)
        {
            sb.AppendLine(isAsync 
                ? $"return {value.ClassName}.Instance.DbDataReaderQueryFirstOrDefaultAsync(reader, cancellationToken);"
                : $"return {value.ClassName}.Instance.DbDataReaderQueryFirstOrDefault(reader);");
        }

        private static void GenerateQueryMethod(StringBuilder sb, GeneratedMapping value, bool isAsync)
        {
            sb.AppendLine(isAsync
                ? $"return {value.ClassName}.Instance.DbDataReaderQueryAsync(reader, cancellationToken);"
                : $"return {value.ClassName}.Instance.DbDataReaderQuery(reader);");
        }

        private static void GenerateExecuteReaderMethod(StringBuilder sb, GeneratedMapping value, IInvocationOperation op, bool isAsync)
        {
            var isDbConnection = op.IsDbConnection();
            if (isDbConnection) 
            {
                sb.AppendLine("var cmd = connection.CreateCommand();");
                sb.AppendLine("cmd.CommandText = sql;");
                sb.AppendLine("cmd.CommandType = commandType;");
            }
            sb.AppendLine($"{value.ClassName}.Instance.SetParams(cmd, args);");
            sb.AppendLine(isAsync 
                ? "return cmd.ExecuteReaderAsync(behavior, cancellationToken);"
                : "return cmd.ExecuteReader(behavior);");
        }

        private static void GenerateSetParamsMethod(StringBuilder sb,GeneratedMapping value)
        {
            sb.AppendLine($"{value.ClassName}.Instance.SetParams(cmd, args);");
        }
    }
}
