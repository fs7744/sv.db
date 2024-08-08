namespace UT
{
    public class EnumerableExtensionsTest
    {
        [Theory]
        [InlineData(198, 6)]
        [InlineData(198, 60)]
        public void Page(int count, int pageSize)
        {
            Assert.Equal( Enumerable.Range(0, count).Sum(), Enumerable.Range(0, count).ToList().Page(pageSize).SelectMany(i => i).Sum());
        }
    }
}