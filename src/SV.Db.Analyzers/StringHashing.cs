using System;
using System.Buffers;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;

namespace SlowestEM.Generator
{
    public static partial class StringHashing
    {
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static uint RotateLeft(uint value, int offset)
            => (value << offset) | (value >> (32 - offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static ulong RotateLeft(ulong value, int offset)
            => (value << offset) | (value >> (64 - offset));

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        internal static bool AllCharsInUInt32AreAscii(uint value)
        {
            return (value & ~0x007F_007Fu) == 0;
        }

        private const uint NormalizeToLowercase = 0x0020_0020u;

        internal static unsafe int GetNonRandomizedHashCodeOrdinalIgnoreCase(ReadOnlySpan<char> span)
        {
            uint hash1 = (5381 << 16) + 5381;
            uint hash2 = hash1;

            uint p0, p1;
            int length = span.Length;

            fixed (char* src = &MemoryMarshal.GetReference(span))
            {
                uint* ptr = (uint*)src;

            LengthSwitch:
                switch (length)
                {
                    default:
                        do
                        {
                            p0 = Unsafe.ReadUnaligned<uint>(ptr);
                            p1 = Unsafe.ReadUnaligned<uint>(ptr + 1);
                            if (!AllCharsInUInt32AreAscii(p0 | p1))
                            {
                                goto NotAscii;
                            }

                            length -= 4;
                            hash1 = (RotateLeft(hash1, 5) + hash1) ^ (p0 | NormalizeToLowercase);
                            hash2 = (RotateLeft(hash2, 5) + hash2) ^ (p1 | NormalizeToLowercase);
                            ptr += 2;
                        }
                        while (length >= 4);
                        goto LengthSwitch;

                    case 3:
                        p0 = Unsafe.ReadUnaligned<uint>(ptr);
                        p1 = *(char*)(ptr + 1);
                        if (!BitConverter.IsLittleEndian)
                        {
                            p1 <<= 16;
                        }

                        if (!AllCharsInUInt32AreAscii(p0 | p1))
                        {
                            goto NotAscii;
                        }

                        hash1 = (RotateLeft(hash1, 5) + hash1) ^ (p0 | NormalizeToLowercase);
                        hash2 = (RotateLeft(hash2, 5) + hash2) ^ (p1 | NormalizeToLowercase);
                        break;

                    case 2:
                        p0 = Unsafe.ReadUnaligned<uint>(ptr);
                        if (!AllCharsInUInt32AreAscii(p0))
                        {
                            goto NotAscii;
                        }

                        hash2 = (RotateLeft(hash2, 5) + hash2) ^ (p0 | NormalizeToLowercase);
                        break;

                    case 1:
                        p0 = *(char*)ptr;
                        if (!BitConverter.IsLittleEndian)
                        {
                            p0 <<= 16;
                        }

                        if (p0 > 0x7f)
                        {
                            goto NotAscii;
                        }

                        hash2 = (RotateLeft(hash2, 5) + hash2) ^ (p0 | NormalizeToLowercase);
                        break;

                    case 0:
                        break;
                }
            }

            return (int)(hash1 + (hash2 * 1566083941));

        NotAscii:
            return GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(hash1, hash2, span.Slice(span.Length - length));
        }

        private static unsafe int GetNonRandomizedHashCodeOrdinalIgnoreCaseSlow(uint hash1, uint hash2, ReadOnlySpan<char> str)
        {
            int length = str.Length;

            // We allocate one char more than the length to accommodate a null terminator.
            // That lets the reading always be performed two characters at a time, as odd-length
            // inputs will have a final terminator to backstop the last read.
            char[] borrowedArr = null;
            Span<char> scratch = (uint)length < 256 ?
                stackalloc char[256] :
                (borrowedArr = ArrayPool<char>.Shared.Rent(length + 1));

            scratch[length] = '\0';

            // Duplicate the main loop, can be removed once JIT gets "Loop Unswitching" optimization
            fixed (char* src = scratch)
            {
                uint* ptr = (uint*)src;
                while (length > 2)
                {
                    length -= 4;
                    hash1 = (RotateLeft(hash1, 5) + hash1) ^ (ptr[0] | NormalizeToLowercase);
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ (ptr[1] | NormalizeToLowercase);
                    ptr += 2;
                }

                if (length > 0)
                {
                    hash2 = (RotateLeft(hash2, 5) + hash2) ^ (ptr[0] | NormalizeToLowercase);
                }
            }

            if (borrowedArr != null)
            {
                ArrayPool<char>.Shared.Return(borrowedArr);
            }

            return (int)(hash1 + (hash2 * 1566083941));
        }

        public static int Hash(string value)
        {
            return GetNonRandomizedHashCodeOrdinalIgnoreCase(value.AsSpan());
        }
    }
}