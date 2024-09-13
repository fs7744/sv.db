using SV;
using System.Xml.Linq;

namespace UT
{
    public class StringHashingTest
    {
        [Fact]
        public void NormalizedHash()
        {
            var hash = typeof(string).GetMethod("GetNonRandomizedHashCodeOrdinalIgnoreCase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).CreateDelegate<Func<string, int>>();
            Assert.Equal("   ".HashOrdinalIgnoreCase(), "   ".HashOrdinalIgnoreCase());
            Assert.Equal(" asds dsff".HashOrdinalIgnoreCase(), " ASDS dsff".HashOrdinalIgnoreCase());
            Assert.Equal(1666770079, " asds dsff".HashOrdinalIgnoreCase());
            Assert.Equal(1666770079, hash(" asds dsff"));
            Assert.Equal(" asds dsff".HashOrdinalIgnoreCase(), hash(" ASDS dsff"));
            var a = StringHashing.HashOrdinalIgnoreCase("Int32");
        }
    }
}