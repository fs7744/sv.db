using Microsoft.CodeAnalysis;
using System;

namespace SV.Db.Analyzers
{
    [Generator(LanguageNames.CSharp)]
    public class CodeGenerator : IIncrementalGenerator
    {
        public void Initialize(IncrementalGeneratorInitializationContext context)
        {
        }
    }
}
