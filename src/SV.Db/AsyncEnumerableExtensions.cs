namespace SV.Db
{
    public static class AsyncEnumerableExtensions
    {
        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> ae, CancellationToken cancellationToken = default)
        {
            List<T> result = new List<T>();
            await foreach (var item in ae.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                result.Add(item);
            }
            return result;
        }

        public static async Task<List<T>> ToListAsync<T>(this IAsyncEnumerable<T> ae, Func<T, T> func, CancellationToken cancellationToken = default)
        {
            List<T> result = new List<T>();
            await foreach (var item in ae.WithCancellation(cancellationToken).ConfigureAwait(false))
            {
                result.Add(func(item));
            }
            return result;
        }
    }
}