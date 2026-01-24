using System.Buffers;
using System.Data;
using System.Runtime.InteropServices;

namespace SV.Db
{
    public struct ReaderState
    {
        public IDataReader? Reader;
        public int[]? Tokens;
        public int FieldCount;

        public ValueTask DisposeAsync()
        {
            Dispose();
            return default;
        }

        public void Dispose()
        {
            Return();
            Reader?.Dispose();
        }

        public int[] GetTokens()
        {
            FieldCount = Reader!.FieldCount;
            if (Tokens is null || Tokens.Length < FieldCount)
            {
                if (Tokens is not null) ArrayPool<int>.Shared.Return(Tokens);
                Tokens = ArrayPool<int>.Shared.Rent(FieldCount);
            }
            return Tokens;
        }

        public readonly ReadOnlySpan<int> RTokens
        {
            get
            {
#pragma warning disable CS8604 // Possible null reference argument.
                return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(Tokens), FieldCount);
#pragma warning restore CS8604 // Possible null reference argument.
            }
        }

        public void Return()
        {
            if (Tokens is not null)
            {
                ArrayPool<int>.Shared.Return(Tokens);
                Tokens = null;
                FieldCount = 0;
            }
        }
    }
}