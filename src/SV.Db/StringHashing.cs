using System.Runtime.CompilerServices;

namespace SV.Db
{
    public static class StringHashing
    {
        public static uint SlowNonRandomizedHash(this string? value)
        {
            uint hash = 0;
            if (!string.IsNullOrEmpty(value))
            {
                hash = 2166136261u;
                foreach (char c in value!)
                {
                    hash = (char.ToLowerInvariant(c) ^ hash) * 16777619;
                }
            }
            return hash;
        }

        [UnsafeAccessor(UnsafeAccessorKind.Method, Name = "GetNonRandomizedHashCodeOrdinalIgnoreCase")]
        public static extern int NonRandomizedHash(this string c);
    }
}