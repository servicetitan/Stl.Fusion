namespace Stl.Rpc.Caching;

public static class RpcCacheInfoCaptureExt
{
    public static async ValueTask<(RpcCacheKey? Key, TextOrBytes? Data)> GetKeyAndData(
        this RpcCacheInfoCapture? cacheInfoCapture,
        CancellationToken cancellationToken = default)
    {
        if (cacheInfoCapture == null || !cacheInfoCapture.HasKeyAndData(out var key, out var dataSource))
            return (null, null);

        try {
            var data = await dataSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return (key, data);
        }
        catch (Exception e) when (!e.IsCancellationOf(cancellationToken)) {
            return (key, null);
        }
    }
}
