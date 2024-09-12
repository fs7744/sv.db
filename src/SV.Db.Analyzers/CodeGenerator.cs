using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;

namespace SV.Db.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            var nodes = context.SyntaxProvider.CreateSyntaxProvider(FilterFunc, TransformFunc)
                .Where(x => x is not null)
                .Select((x, _) => x!);
            var combined = context.CompilationProvider.Combine(nodes.Collect());
            context.RegisterImplementationSourceOutput(combined, Generate);
        }

        private void Generate(SourceProductionContext context, (Compilation Compilation, ImmutableArray<SourceState> Sources) state)
        {
            try
            {
                var code = GenerateCode(state.Sources);
                context.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", code);
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
            }
        }

        private string GenerateCode(ImmutableArray<SourceState> sources)
        {
            var sb = new StringBuilder();
            var omap = new Dictionary<string, List<SourceState>>();
            foreach (var item in sources)
            {
                if (item.Args == null && item.ReturnType == null) continue;

                if (item.NeedGenerateArgs())
                {
                    Add(omap, item.Args.Type.ToDisplayString(), item);
                }

                if (item.NeedGenerateReturnType())
                {
                    Add(omap, item.ReturnType.ToDisplayString(), item);
                }
            }
#if DEBUG
            sb.Insert(0, $"// total: {omap.Count} \r\n\r\n" + string.Join("", omap.Select(i => $"// {i.Key}: {i.Value.Count} \r\n{string.Join("", i.Value.Select(i => i.ToString()))}\r\n")));
#endif
            return sb.ToString();

            static void Add(Dictionary<string, List<SourceState>> dict, string key, SourceState source)
            {
                if (!dict.TryGetValue(key, out var value))
                {
                    value = new List<SourceState>();
                    dict.Add(key, value);
                }
                value.Add(source);
            }
        }

        private bool FilterFunc(SyntaxNode node, CancellationToken token)
        {
            if (node is InvocationExpressionSyntax ie && ie.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
            {
                return FilterFuncName(ma.Name.ToString());
            }

            return false;
        }

        private bool FilterFuncName(string funcName)
        {
            return funcName.StartsWith("Execute") || funcName.StartsWith("Query") || funcName.StartsWith("SetParams");
        }

        private SourceState TransformFunc(GeneratorSyntaxContext ctx, CancellationToken token)
        {
            try
            {
                if (ctx.Node is not InvocationExpressionSyntax ie
                    || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op
                    || !IsDbFunc(op))
                {
                    return null;
                }
                bool hasArgs = false;
                bool hasResultType = false;
                var method = op.TargetMethod;
                switch (method.Name)
                {
                    case "ExecuteNonQuery":
                    case "ExecuteNonQueryAsync":
                    case "ExecuteNonQuerys":
                    case "ExecuteNonQuerysAsync":
                    case "SetParams":
                    case "ExecuteReader":
                    case "ExecuteReaderAsync":
                        hasArgs = true;
                        break;

                    case "ExecuteQuery":
                    case "ExecuteQueryAsync":
                    case "ExecuteQueryFirstOrDefault":
                    case "ExecuteQueryFirstOrDefaultAsync":
                    case "ExecuteScalar":
                    case "ExecuteScalarAsync":
                        hasArgs = true;
                        hasResultType = true;
                        break;

                    case "QueryFirstOrDefault":
                    case "QueryFirstOrDefaultAsync":
                    case "Query":
                    case "QueryAsync":
                        hasResultType = true;
                        break;

                    default:
                        return null;
                }

                IOperation argExpression = null;
                foreach (var arg in op.Arguments)
                {
                    switch (arg.Parameter?.Name)
                    {
                        case "args":
                            if (hasArgs && arg.Value is not IDefaultValueOperation)
                            {
                                var expr = arg.Value;
                                while (expr is IConversionOperation conv && expr.Type?.SpecialType == SpecialType.System_Object)
                                {
                                    expr = conv.Operand;
                                }
                                if (!(expr.ConstantValue.HasValue && expr.ConstantValue.Value is null))
                                {
                                    argExpression = expr;
                                }
                                if (argExpression != null && argExpression.Type.SpecialType == SpecialType.System_Object)
                                {
                                    argExpression = null;
                                }
                            }
                            break;

                        default: break;
                    }
                }

                return new SourceState()
                {
                    IsAsync = method.Name.EndsWith("Async"),
                    Invocation = op,
                    Args = argExpression,
                    ReturnType = hasResultType ? op.GetResultType() : null,
                };
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
                return null;
            }
        }

        private bool IsDbFunc(IInvocationOperation op)
        {
            var method = op?.TargetMethod;
            if (method is null || !method.IsExtensionMethod)
            {
                return false;
            }
            var type = method.ContainingType;
            if (type is not { Name: "CommandExtensions", ContainingNamespace: { Name: "Db", IsGlobalNamespace: false, ContainingNamespace: { Name: "SV", IsGlobalNamespace: false, ContainingNamespace.IsGlobalNamespace: true } } })
            {
                return false;
            }
            return true;
        }
    }
}