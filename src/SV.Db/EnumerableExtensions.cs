namespace System.Linq
{
    public static partial class EnumerableExtensions
    {
        public static IEnumerable<IEnumerable<T>> Chunk<T>(this List<T> source, int pageSize)
        {
            ArgumentNullException.ThrowIfNull(source, nameof(source));
            
            if (source.Count <= pageSize)
            {
                yield return source;
            }
            else
            {
                var totalCount = (int)Math.Ceiling(source.Count * 1.0 / pageSize);
                for (int i = 0; i <= totalCount; i++)
                {
                    yield return source.Skip(pageSize * i).Take(pageSize);
                }
            }
        }

        public static IEnumerable<IEnumerable<T>> Page<T>(this IEnumerable<T> source, int pageSize)
        {
            if (source is List<T> s)
            {
                return s.Chunk(pageSize);
            }
            else
            {
                return source.Chunk(pageSize);
            }
        }

        public static bool IsNullOrEmpty<T>(this List<T> source)
        {
            return source == null || source.Count == 0;
        }

        public static bool IsNullOrEmpty<T>(this T[] source)
        {
            return source == null || source.Length == 0; ;
        }
        public static bool IsNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source == null 
                || (source is IList<T> s && s.Count == 0)
                || !source.GetEnumerator().MoveNext();
        }

        public static bool IsNotNullOrEmpty<T>(this List<T> source)
        {
            return source != null && source.Count > 0;
        }

        public static bool IsNotNullOrEmpty<T>(this T[] source)
        {
            return source != null && source.Length > 0;
        }

        public static bool IsNotNullOrEmpty<T>(this IEnumerable<T> source)
        {
            return source != null && 
                ((source is IList<T> s && s.Count > 0)
                || source.GetEnumerator().MoveNext());
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