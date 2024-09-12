using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.Operations;
using System.Collections.Frozen;
using System.Collections.Generic;

namespace SV.Db.Analyzers
{
    public class SourceState
    {
        internal static readonly FrozenSet<SpecialType> NoGenerateSpecialType = new HashSet<SpecialType>()
        {
            SpecialType.System_Object, SpecialType.System_Enum, SpecialType.System_MulticastDelegate, SpecialType.System_Delegate, SpecialType.System_Void, SpecialType.System_Boolean, SpecialType.System_Char, SpecialType.System_SByte,
            SpecialType.System_Byte, SpecialType.System_Int16, SpecialType.System_UInt16, SpecialType.System_Int32, SpecialType.System_UInt32, SpecialType.System_Int64,
            SpecialType.System_UInt64, SpecialType.System_Decimal, SpecialType.System_Single, SpecialType.System_Double, SpecialType.System_String, SpecialType.System_IntPtr,
            SpecialType.System_UIntPtr,SpecialType.System_Nullable_T, SpecialType.System_DateTime
        }.ToFrozenSet();

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
            return Args != null && !NoGenerateSpecialType.Contains(Args.Type.SpecialType) && Args.Type.TypeKind != TypeKind.Enum && Args.Type.TypeKind != TypeKind.Dynamic;
        }

        public bool NeedGenerateReturnType()
        {
            return ReturnType != null && !NoGenerateSpecialType.Contains(ReturnType.SpecialType) && ReturnType.TypeKind != TypeKind.Enum && ReturnType.TypeKind != TypeKind.Dynamic;
        }
    }
}