using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Immutable;
using System.Diagnostics;
using System.Linq;
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

        private void Generate(SourceProductionContext context, (Compilation Compilation, ImmutableArray<SourceState> Sources) tuple)
        {
            try
            {
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
            switch (funcName)
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
                    return true;

                default:
                    return false;
            }
        }

        private SourceState TransformFunc(GeneratorSyntaxContext ctx, CancellationToken token)
        {
            try
            {
                if (ctx.Node is not InvocationExpressionSyntax ie
                    || ctx.SemanticModel.GetOperation(ie) is not IInvocationOperation op
                    || IsDbFunc(op))

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
            if (type is not { Name: "CommandExtensions", ContainingNamespace: { Name: "SV.Db", ContainingNamespace.IsGlobalNamespace: true } })
            {
                return false;
            }
            return true;
        }
    }
}