using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using System.Linq;
using System.Threading;

namespace SV.Db.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
            //var nodes = context.SyntaxProvider.CreateSyntaxProvider(FilterFunc, TransformFunc)
            //    .Where(x => x is not null)
            //        .Select((x, _) => x!);
            //var combined = context.CompilationProvider.Combine(nodes.Collect());
            //context.RegisterImplementationSourceOutput(combined, Generate);
        }

        private bool FilterFunc(SyntaxNode node, CancellationToken token)
        {
            if (node is InvocationExpressionSyntax ie && ie.ChildNodes().FirstOrDefault() is MemberAccessExpressionSyntax ma)
            {
                return ma.Name.ToString().StartsWith("TestInterceptor");
            }

            return false;
        }
    }
}