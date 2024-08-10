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

        public Span<int> GetTokens()
        {
            FieldCount = Reader!.FieldCount;
            if (Tokens is null || Tokens.Length < FieldCount)
            {
                if (Tokens is not null) ArrayPool<int>.Shared.Return(Tokens);
                Tokens = ArrayPool<int>.Shared.Rent(FieldCount);
            }
            return MemoryMarshal.CreateSpan(ref MemoryMarshal.GetArrayDataReference(Tokens), FieldCount);
        }

        public readonly ReadOnlySpan<int> RTokens
        {
            get
            {
                return MemoryMarshal.CreateReadOnlySpan(ref MemoryMarshal.GetArrayDataReference(Tokens), FieldCount);
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