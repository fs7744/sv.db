#pragma warning disable CS8604 // Possible null reference argument.
#pragma warning disable CS8600 // Converting null literal or possible null value to non-nullable type.

namespace UT
{
    public class EnumerableExtensionsTest
    {
        [Theory]
        [InlineData(198, 6)]
        [InlineData(198, 60)]
        public void Page(int count, int pageSize)
        {
            Assert.Equal(Enumerable.Range(0, count).Sum(), Enumerable.Range(0, count).ToList().Page(pageSize).SelectMany(i => i).Sum());
        }

        [Fact]
        public void IsNullOrEmpty()
        {
            List<int> list = null;
            Assert.True(list.IsNullOrEmpty());
            list = new List<int>();
            Assert.True(list.IsNullOrEmpty());
            list.Add(0);
            Assert.False(list.IsNullOrEmpty());

            int[] array = null;
            Assert.True(array.IsNullOrEmpty());
            array = new int[0];
            Assert.True(array.IsNullOrEmpty());
            array = list.ToArray();
            Assert.False(array.IsNullOrEmpty());

            IEnumerable<int> enumerable = null;
            Assert.True(enumerable.IsNullOrEmpty());
            enumerable = list.Where(i => i > 1);
            Assert.True(enumerable.IsNullOrEmpty());
            list.Add(3);
            Assert.False(enumerable.IsNullOrEmpty());
        }

        [Fact]
        public void IsNotNullOrEmpty()
        {
            List<int> list = null;
            Assert.False(list.IsNotNullOrEmpty());
            list = new List<int>();
            Assert.False(list.IsNotNullOrEmpty());
            list.Add(0);
            Assert.True(list.IsNotNullOrEmpty());

            int[] array = null;
            Assert.False(array.IsNotNullOrEmpty());
            array = new int[0];
            Assert.False(array.IsNotNullOrEmpty());
            array = list.ToArray();
            Assert.True(array.IsNotNullOrEmpty());

            IEnumerable<int> enumerable = null;
            Assert.False(enumerable.IsNotNullOrEmpty());
            enumerable = list.Where(i => i > 1);
            Assert.False(enumerable.IsNotNullOrEmpty());
            list.Add(3);
            Assert.True(enumerable.IsNotNullOrEmpty());
        }
    }
}

#pragma warning restore CS8604 // Possible null reference argument.
#pragma warning restore CS8600 // Converting null literal or possible null value to non-nullable type.