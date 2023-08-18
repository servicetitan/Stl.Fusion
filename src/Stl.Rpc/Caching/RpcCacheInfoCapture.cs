namespace Stl.Rpc.Caching;

public enum RpcCacheInfoCaptureMode
{
    KeyAndResult = 0,
    KeyOnly,
}

public class RpcCacheInfoCapture
{
    public readonly RpcCacheInfoCaptureMode CaptureMode;
    public RpcCacheKey? Key;
    public TaskCompletionSource<TextOrBytes>? ResultSource;

    public RpcCacheInfoCapture(RpcCacheInfoCaptureMode captureMode = default)
    {
        CaptureMode = captureMode;
        if (captureMode == RpcCacheInfoCaptureMode.KeyAndResult)
            ResultSource = new();
    }

    public async ValueTask<RpcCacheEntry?> GetEntry(CancellationToken cancellationToken = default)
    {
        if (ReferenceEquals(Key, null) ||  ResultSource == null)
            return null;

        try {
            var result = await ResultSource.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
            return new RpcCacheEntry(Key, result);
        }
        catch (OperationCanceledException) {
            if (cancellationToken.IsCancellationRequested)
                throw;

            return null;
        }
        catch (Exception) {
            return null;
        }
    }
}
