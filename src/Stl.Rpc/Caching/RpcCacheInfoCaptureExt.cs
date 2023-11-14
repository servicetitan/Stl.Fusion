namespace Stl.Rpc.Caching;

public static class RpcCacheInfoCaptureExt
{
    public static async ValueTask<(RpcCacheKey? Key, TextOrBytes? Data)> GetKeyAndData(
        this RpcCacheInfoCapture? cacheInfoCapture,
        CancellationToken cancellationToken)
    {
        if (cacheInfoCapture == null || !cacheInfoCapture.HasKeyAndData(out var key, out var dataSource))
            return (null, null);

        try {
            // .WaitAsync(cancellationToken) is unnecessary here:
            // dataSource is reliably cancelled on cancellation of the very same token.
            var data = await dataSource.Task.ConfigureAwait(false);
            return (key, data);
        }
        catch (Exception e) when (!e.IsCancellationOf(cancellationToken)) {
            return (key, null);
        }
    }
}
