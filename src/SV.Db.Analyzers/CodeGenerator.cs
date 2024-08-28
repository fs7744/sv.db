using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
using System.Linq.Expressions;
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
                context.AddSource((state.Compilation.AssemblyName ?? "package") + ".generated.cs", $"// {state.Sources.Length} \r\n" + string.Join("", state.Sources.Select(i => i.ToString())));
            }
            catch (Exception ex)
            {
                Debug.Fail(ex.Message);
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

                var method = op.TargetMethod;
                switch (method.Name)
                {
                    case "ExecuteNonQuery":
                    case "ExecuteNonQueryAsync":
                    case "ExecuteNonQuerys":
                    case "ExecuteNonQuerysAsync":
                    case "ExecuteQuery":
                    case "ExecuteQueryAsync":
                    case "ExecuteQueryFirstOrDefault":
                    case "ExecuteQueryFirstOrDefaultAsync":
                    case "ExecuteReader":
                    case "ExecuteReaderAsync":
                    case "QueryFirstOrDefault":
                    case "QueryFirstOrDefaultAsync":
                    case "Query":
                    case "QueryAsync":
                    case "ExecuteScalar":
                    case "ExecuteScalarAsync":
                    case "SetParams":
                        {
                            IOperation argExpression = null;
                            foreach (var arg in op.Arguments)
                            {
                                switch (arg.Parameter?.Name)
                                {
                                    case "args":
                                        if (arg.Value is not IDefaultValueOperation)
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
                                Args = argExpression
                            };
                        }

                    default:
                        return null;
                }
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