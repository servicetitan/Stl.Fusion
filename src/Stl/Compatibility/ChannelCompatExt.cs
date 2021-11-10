#if NETSTANDARD2_0

// ReSharper disable once CheckNamespace
namespace System.Threading.Channels;

public static class ChannelCompatExt
{
    public static async IAsyncEnumerable<T> ReadAllAsync<T>(
        this ChannelReader<T> reader,
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        while (await reader.WaitToReadAsync(cancellationToken).ConfigureAwait(false))
        while (reader.TryRead(out var item))
            yield return item;
    }
}

#endif
