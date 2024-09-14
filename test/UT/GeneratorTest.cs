using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel;
using System.Data.Common;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Runtime.Serialization;
using System.Text;
using System.Threading.Tasks;
using SV.Db.Analyzers;
using System.Globalization;
using Xunit.Abstractions;
using UT.GeneratorTestCases;

namespace UT
{
    internal static class GeneratorTest
    {
        internal static readonly CSharpParseOptions ParseOptionsLatestLangVer = CSharpParseOptions.Default
        .WithLanguageVersion(LanguageVersion.Latest)
        .WithPreprocessorSymbols(new string[] {
#if NETFRAMEWORK
        "NETFRAMEWORK",
#endif
#if NET40_OR_GREATER
        "NET40_OR_GREATER",
#endif
#if NET48_OR_GREATER
        "NET48_OR_GREATER",
#endif
#if NET6_0_OR_GREATER
        "NET6_0_OR_GREATER",
#endif
#if NET7_0_OR_GREATER
        "NET7_0_OR_GREATER",
#endif
#if DEBUG
        "DEBUG",
#endif
#if RELEASE
        "RELEASE",
#endif
    })
    .WithFeatures(new[] { new KeyValuePair<string, string>("InterceptorsPreviewNamespaces", "SV.Db") });

        public static Compilation CreateCompilation(string source)
           => CSharpCompilation.Create("compilation",
               syntaxTrees: new[] { CSharpSyntaxTree.ParseText(source, ParseOptionsLatestLangVer) },
               references: new[] {
                   MetadataReference.CreateFromFile(typeof(Binder).Assembly.Location),
#if !NET48
                   MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Data").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("netstandard").Location),
                   MetadataReference.CreateFromFile(Assembly.Load("System.Collections").Location),
                   MetadataReference.CreateFromFile(typeof(System.ComponentModel.DataAnnotations.Schema.ColumnAttribute).Assembly.Location),
#endif
                   MetadataReference.CreateFromFile(typeof(Console).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DbConnection).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ValueTask<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Component).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ImmutableList<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(ImmutableArray<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IAsyncEnumerable<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Span<int>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IgnoreDataMemberAttribute).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SV.Enums).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(SV.Db.DBUtils).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(DynamicAttribute).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(IValidatableObject).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(TestData).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(Assert).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(System.Collections.Concurrent.ConcurrentDictionary<,>).Assembly.Location),
                   MetadataReference.CreateFromFile(typeof(System.Collections.ObjectModel.ReadOnlyDictionary<,>).Assembly.Location),
               },
               options: new CSharpCompilationOptions(OutputKind.ConsoleApplication, allowUnsafe: true));

        public static (Compilation? Compilation, GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics, int errorCount, string diagnosticsTo) Run<T>(string code) where T : class, IIncrementalGenerator, new()
        {
            Compilation inputCompilation = CreateCompilation(code);
            StringBuilder diagnosticsTo = new StringBuilder();
            var ierr = ShowDiagnostics("Input code", inputCompilation, diagnosticsTo, "CS8795", "CS1701", "CS1702", "CS8019");
            var generator = new T();
            GeneratorDriver driver = CSharpGeneratorDriver.Create(new[] { generator.AsSourceGenerator() }, parseOptions: ParseOptionsLatestLangVer);
            driver = driver.RunGeneratorsAndUpdateCompilation(inputCompilation, out var outputCompilation, out var diagnostics);
            GeneratorDriverRunResult runResult = driver.GetRunResult();
            var errorCount = ShowDiagnostics("Output code", outputCompilation, diagnosticsTo, "CS1701", "CS1702", "CS8019");
            return (outputCompilation, runResult, diagnostics, errorCount, diagnosticsTo.ToString());
        }

        public static (Compilation? Compilation, GeneratorDriverRunResult Result, ImmutableArray<Diagnostic> Diagnostics, int errorCount, string diagnosticsTo) CodeGenerate(string code)
        {
            return Run<CodeGenerator>(code);
        }

        private static int ShowDiagnostics(string caption, Compilation compilation, StringBuilder? diagnosticsTo, params string[] ignore)
        {
            if (diagnosticsTo is null) return 0; // nothing useful to do!
            void Output(string message, bool force = false)
            {
                if (force || !string.IsNullOrWhiteSpace(message))
                {
                    diagnosticsTo?.AppendLine(message.Replace('\\', '/')); // need to normalize paths
                }
            }
            int errorCountTotal = 0;
            foreach (var tree in compilation.SyntaxTrees)
            {
                var rawDiagnostics = compilation.GetSemanticModel(tree).GetDiagnostics();
                var diagnostics = Normalize(rawDiagnostics, ignore);
                errorCountTotal += rawDiagnostics.Count(x => x.Severity == DiagnosticSeverity.Error);

                if (diagnostics.Any())
                {
                    Output($"{caption} has {diagnostics.Count} diagnostics :");
                    foreach (var d in diagnostics)
                    {
                        OutputDiagnostic(d);
                    }
                }
            }
            return errorCountTotal;

            void OutputDiagnostic(Diagnostic d)
            {
                Output("", true);
                var loc = d.Location.GetMappedLineSpan();
                Output($"{d.Severity} {d.Id} {loc.Path} L{loc.StartLinePosition.Line + 1} C{loc.StartLinePosition.Character + 1}");
                Output(d.GetMessage(CultureInfo.InvariantCulture));
            }
        }

        private static List<Diagnostic> Normalize(ImmutableArray<Diagnostic> diagnostics, string[] ignore) => (
           from d in diagnostics
           where !ignore.Contains(d.Id)
           let loc = d.Location
           orderby loc.SourceTree?.FilePath, loc.SourceSpan.Start, d.Id, d.ToString()
           select d).ToList();
    }

    public abstract class GeneratorTestBase
    {
        protected readonly ITestOutputHelper output;

        public GeneratorTestBase(ITestOutputHelper output)
        {
            this.output = output;
        }

        protected (Compilation? Compilation, GeneratorDriverRunResult Result, int errorCount) TestGenerate(string code)
        {
            (var compilation, var result, var diagnostics, var errorCount, var diagnosticsTo) = GeneratorTest.CodeGenerate(code);
            output.WriteLine(diagnosticsTo);
            return (compilation, result, errorCount);
        }
    }

    public class GeneratorTestCase : GeneratorTestBase
    {
        public GeneratorTestCase(ITestOutputHelper output) : base(output)
        {
        }

        public static IEnumerable<object[]> GetFiles() =>
           from path in Directory.GetFiles("GeneratorTestCases", "*.cs", SearchOption.AllDirectories)
           where path.EndsWith("TestCase.cs", StringComparison.OrdinalIgnoreCase)
           select new object[] { path };

        [Theory, MemberData(nameof(GetFiles))]
        public void TestGenerateCode(string path)
        {
            var code = File.ReadAllText(path);
            code = code.Substring(0, code.IndexOf("public void Check(")) + "}}";
            (var compilation, var result, var errorCount) = TestGenerate(code);
            var results = Assert.Single(result.Results);
            var generatedCodeS = results.GeneratedSources.Any() ? results.GeneratedSources.Single().SourceText : null;
            var generatedCode = generatedCodeS == null ? "" : generatedCodeS.ToString();
            //CSharpSyntaxTree.ParseText(generatedCodeS).GetRoot().NormalizeWhitespace().SyntaxTree.GetText().ToString();
            output.WriteLine(generatedCode);
            Assert.Equal(0, errorCount);
            dynamic a = Activator.CreateInstance(this.GetType().Assembly.GetName().FullName, $"UT.GeneratorTestCases.{Path.GetFileName(path).Replace(".cs", "")}").Unwrap();
            a.Check(generatedCode);
        }
    }
}