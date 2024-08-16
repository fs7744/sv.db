using System.Diagnostics.CodeAnalysis;
using System.Runtime.CompilerServices;

namespace SV.Db
{
    internal static class ThrowHelper
    {
        [DoesNotReturn]
        [MethodImpl(MethodImplOptions.NoInlining)]
        internal static void ThrowNotSupportedException()
        {
            throw new NotSupportedException();
        }
    }
}