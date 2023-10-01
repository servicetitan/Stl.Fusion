namespace Stl.Rpc.Caching;

public enum RpcCacheInfoCaptureMode
{
    KeyAndResult = 0,
    KeyOnly,
}

public sealed class RpcCacheInfoCapture
{
    public readonly RpcCacheInfoCaptureMode CaptureMode;
    public RpcCacheKey? Key;
    public TaskCompletionSource<TextOrBytes>? ResultSource; // Must never be an error, but can be cancelled

    public RpcCacheInfoCapture(RpcCacheInfoCaptureMode captureMode = default)
    {
        CaptureMode = captureMode;
        if (captureMode == RpcCacheInfoCaptureMode.KeyAndResult)
            ResultSource = new();
    }

    public async ValueTask<RpcCacheEntry?> GetEntry()
    {
        if (ReferenceEquals(Key, null) ||  ResultSource == null)
            return null;

        try {
            var result = await ResultSource.Task.ConfigureAwait(false);
            return new RpcCacheEntry(Key, result);
        }
        catch (OperationCanceledException) {
            return null;
        }
    }
}
