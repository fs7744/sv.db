namespace System.Linq
{
    public static partial class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Page<T>(this IEnumerable<T> source, int pageSize)
        {
            if (source == null)
                return null;
            else if (source is IList<T> || source is ICollection<T>)
            {
                return CollectionPage(source, pageSize);
            }
            else
            {
                return source.Chunk(pageSize);
            }
        }

        private static IEnumerable<IEnumerable<T>> CollectionPage<T>(IEnumerable<T> source, int pageSize)
        {
            var t = source.Count();
            if (t <= pageSize)
            {
                yield return source;
            }
            else
            {
                var totalCount = (int)Math.Ceiling(t * 1.0 / pageSize);
                for (int i = 0; i <= totalCount; i++)
                {
                    yield return source.Skip(pageSize * i).Take(pageSize);
                }
            }
            
        }

        public static List<T> AsList<T>(this IEnumerable<T>? source) => source switch
        {
            null => null!,
            List<T> list => list,
            _ => Enumerable.ToList(source),
        };

        public static ICollection<T> AsCollection<T>(this IEnumerable<T>? source) => source switch
        {
            null => null!,
            T[] array => array,
            List<T> list => list,
            _ => Enumerable.ToArray(source),
        };
    }
}