using SV.Db;

namespace UT
{
    public class StringHashingTest
    {


        [Fact]
        public void NormalizedHash()
        {
            var hash = typeof(string).GetMethod("GetNonRandomizedHashCodeOrdinalIgnoreCase", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance).CreateDelegate<Func<string, int>>();
            Assert.Equal("   ".Hash(), "   ".Hash());
            Assert.Equal(" asds dsff".Hash(), " ASDS dsff".Hash());
            Assert.Equal(1666770079, " asds dsff".Hash());
            Assert.Equal(1666770079, hash(" asds dsff"));
            Assert.Equal(" asds dsff".Hash(), hash(" ASDS dsff"));
        }
    }
}