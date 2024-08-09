using SV.Db;

namespace UT
{
    public class StringHashingTest
    {
        [Fact]
        public void NormalizedHash()
        {
            Assert.Equal("   ".Hash(), "   ".Hash());
            Assert.Equal(" asds dsff".Hash(), " ASDS dsff".Hash());
            Assert.Equal(1666770079, " asds dsff".Hash());
        }
    }
}