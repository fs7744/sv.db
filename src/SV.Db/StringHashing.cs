﻿using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static partial class StringHashing
    {
        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetNonRandomizedHashCodeOrdinalIgnoreCase")]
        public static extern int Hash(this string c);
    }
}