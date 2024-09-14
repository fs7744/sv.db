using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Data;
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

        public static AttributeData? GetAttribute(this ISymbol? symbol, string attributeName)
        {
            if (symbol is not null)
            {
                foreach (var attrib in symbol.GetAttributes())
                {
                    if (attrib.AttributeClass!.Name == attributeName)
                    {
                        return attrib;
                    }
                }
            }

            return null;
        }

        public static ColumnAttributeData? GetColumnAttribute(this ISymbol? symbol)
        {
            var r = GetAttribute(symbol, "ColumnAttribute");
            if (r == null) return null;

            var result = new ColumnAttributeData();
            foreach (var t in r.NamedArguments)
            {
                switch (t.Key)
                {
                    case "Name":
                        result.Name = t.Value.ToCSharpString();
                        break;

                    case "Type":
                        result.Type = t.Value.ToCSharpString();
                        break;

                    case "Direction":
                        result.Direction = t.Value.ToCSharpString();
                        break;

                    case "Precision":
                        result.Precision = t.Value.ToCSharpString();
                        break;

                    case "Scale":
                        result.Scale = t.Value.ToCSharpString();
                        break;

                    case "Size":
                        result.Size = t.Value.ToCSharpString();
                        break;

                    case "CustomConvertMethod":
                        result.CustomConvertMethod = t.Value.ToCSharpString();
                        break;

                    default:
                        break;
                }
            }

            return result;
        }

        internal static bool IsNullable(this ITypeSymbol symbol)
        {
            return symbol is INamedTypeSymbol namedType
                && namedType.IsValueType
                && namedType.IsGenericType
                && namedType.ConstructedFrom?.ToDisplayString() == "System.Nullable<T>";
        }

        internal static string ToFullName(this ITypeSymbol symbol)
        {
            return symbol.ToDisplayString(SymbolDisplayFormat.FullyQualifiedFormat);
        }

        internal static bool IsEnum(this ITypeSymbol symbol)
        {
            return symbol.TypeKind == TypeKind.Enum || (symbol.IsNullable() && symbol is INamedTypeSymbol namedType && namedType.TypeArguments[0].TypeKind == TypeKind.Enum);
        }

        internal static INamedTypeSymbol GetEnumUnderlyingType(this ITypeSymbol symbol)
        {
            if (symbol is INamedTypeSymbol namedType)
            {
                if (symbol.TypeKind == TypeKind.Enum)
                    return namedType.EnumUnderlyingType;
                else if (symbol.IsNullable() && namedType.TypeArguments[0].TypeKind == TypeKind.Enum && namedType.TypeArguments[0] is INamedTypeSymbol tn)
                {
                    return tn.EnumUnderlyingType;
                }
            }
            return null;
        }

        internal static ITypeSymbol GetNullableUnderlyingType(this ITypeSymbol symbol)
        {
            return symbol is INamedTypeSymbol namedType ? namedType.TypeArguments[0] : null;
        }

        internal static ITypeSymbol GetUnderlyingType(this ITypeSymbol symbol)
        {
            var r = symbol;
            if (symbol.IsEnum()) r = symbol.GetEnumUnderlyingType();
            else if (symbol.IsNullable()) r = symbol.GetNullableUnderlyingType();
            if (r == null) r = symbol;
            return r;
        }
    }

    public sealed class ColumnAttributeData
    {
        public string? Name { get; set; }
        public string? Type { get; set; }
        public string? Direction { get; set; }
        public string? Precision { get; set; }
        public string? Scale { get; set; }
        public string? Size { get; set; }
        public string? CustomConvertMethod { get; set; }

        public override string ToString()
        {
            return $"ColumnAttributeData: Name:{Name},Type:{Type},Direction:{Direction},Precision:{Precision},CustomConvertMethod:{CustomConvertMethod},Scale:{Scale},Size:{Size}";
        }
    }
}