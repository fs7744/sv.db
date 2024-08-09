using SV.Db;

namespace UT
{
    public class StringHashingTest
    {
        [Fact]
        public void NormalizedHash()
        {
            Assert.Equal("   ".SlowNonRandomizedHash(), "   ".SlowNonRandomizedHash());
            Assert.Equal(" asds dsff".SlowNonRandomizedHash(), " ASDS dsff".SlowNonRandomizedHash());
            Assert.Equal("   ".NonRandomizedHash(), "   ".NonRandomizedHash());
            Assert.Equal(" asds dsff".NonRandomizedHash(), " ASDS dsff".NonRandomizedHash());
            Assert.Equal(1666770079, " asds dsff".NonRandomizedHash());
        }
    }
}
