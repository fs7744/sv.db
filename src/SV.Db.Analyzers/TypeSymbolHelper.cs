using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SV.Db.Analyzers
{
    public static class TypeSymbolHelper
    {
        public static ITypeSymbol? GetResultType(this IInvocationOperation invocation)
        {
            var typeArgs = invocation.TargetMethod.TypeArguments;
            if (typeArgs.Length == 1)
            {
                return typeArgs[0];
            }
            return null;
        }

        internal static string GetInterceptorFilePath(this SyntaxTree? tree, Compilation compilation)
        {
            if (tree is null) return "";
            return compilation.Options.SourceReferenceResolver?.NormalizePath(tree.FilePath, baseFilePath: null) ?? tree.FilePath;
        }

        public static Location GetMemberLocation(this IInvocationOperation call)
            => GetMemberSyntax(call).GetLocation();

        public static SyntaxNode GetMemberSyntax(this IInvocationOperation call)
        {
            var syntax = call?.Syntax;
            if (syntax is null) return null!; // GIGO

            foreach (var outer in syntax.ChildNodesAndTokens())
            {
                var outerNode = outer.AsNode();
                if (outerNode is not null && outerNode is MemberAccessExpressionSyntax)
                {
                    // if there is an identifier, we want the **last** one - think Foo.Bar.Blap(...)
                    SyntaxNode? identifier = null;
                    foreach (var inner in outerNode.ChildNodesAndTokens())
                    {
                        var innerNode = inner.AsNode();
                        if (innerNode is not null && innerNode is SimpleNameSyntax)
                            identifier = innerNode;
                    }
                    // we'd prefer an identifier, but we'll allow the entire member-access
                    return identifier ?? outerNode;
                }
            }
            return syntax;
        }

        public static IEnumerable<IPropertySymbol> GetAllSettableProperties(this ITypeSymbol typeSymbol)
        {
            var result = typeSymbol
                .GetMembers()
                .Where(s => s.Kind == SymbolKind.Property).Cast<IPropertySymbol>()
                .Where(p => p.SetMethod?.DeclaredAccessibility == Accessibility.Public)
                .Union(typeSymbol.BaseType == null ? new IPropertySymbol[0] : typeSymbol.BaseType.GetAllSettableProperties());

            return result;
        }

        public static IEnumerable<IPropertySymbol> GetAllGettableProperties(this ITypeSymbol typeSymbol)
        {
            var result = typeSymbol
                .GetMembers()
                .Where(s => s.Kind == SymbolKind.Property).Cast<IPropertySymbol>()
                .Where(p => p.GetMethod?.DeclaredAccessibility == Accessibility.Public)
                .Union(typeSymbol.BaseType == null ? new IPropertySymbol[0] : typeSymbol.BaseType.GetAllGettableProperties());

            return result;
        }

        public static IEnumerable<IFieldSymbol> GetAllPublicFields(this ITypeSymbol typeSymbol)
        {
            var result = typeSymbol
                .GetMembers()
                .Where(s => s.Kind == SymbolKind.Field).Cast<IFieldSymbol>()
                .Where(p => p.DeclaredAccessibility == Accessibility.Public)
                .Union(typeSymbol.BaseType == null ? new IFieldSymbol[0] : typeSymbol.BaseType.GetAllPublicFields());

            return result;
        }

        public static string ToDisplayString(this Accessibility declaredAccessibility)
        {
            switch (declaredAccessibility)
            {
                case Accessibility.Private:
                    return "private";

                case Accessibility.Public:
                    return "public";

                case Accessibility.Internal:
                default:
                    return "internal";
            }
        }
    }
}