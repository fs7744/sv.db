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

        public override string ToString()
        {
            return $"// {Invocation?.TargetMethod?.ToDisplayString()} ( arg: {Args?.Type.ToDisplayString()} ) \r\n";
        }
    }
}