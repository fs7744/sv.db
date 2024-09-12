using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System;
using System.Collections.Generic;
using System.Text;

namespace SV.Db.Analyzers
{
    public class SourceState
    {
        public IOperation? Args { get; set; }
        public IInvocationOperation Invocation { get; set; }
        public bool IsAsync { get; set; }
        public ITypeSymbol ReturnType { get; set; }

        public override string ToString()
        {
            return $"// IsAsync:{IsAsync} {Invocation?.TargetMethod?.ToDisplayString()} ( arg: {Args?.Type.ToDisplayString()}, ReturnType: {ReturnType?.ToDisplayString()} ) \r\n";
        }

        public bool NeedGenerateArgs()
        {
            return Args != null;
        }

        public bool NeedGenerateReturnType()
        {
            return ReturnType != null;
        }
    }
}