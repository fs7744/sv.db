namespace SV.Db
{
    public static partial class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Page<T>(this IEnumerable<T> source, int pageSize)
        {
            var totalCount = (int)Math.Ceiling(source.Count() * 1.0 / pageSize);
            for (int i = 0; i <= totalCount; i++)
            {
                yield return source.Skip(pageSize * i).Take(pageSize);
            }
        }
    }
}