namespace Stl.Rpc.Caching;

public class RpcCacheInfoCapture
{
    public RpcCacheKey? Key { get; set; }
    public TaskCompletionSource<TextOrBytes>? ResultSource { get; set; }
    public bool MustCaptureResult => ResultSource != null;

    public RpcCacheInfoCapture(bool mustCaptureResult = true)
    {
        if (mustCaptureResult)
            ResultSource = new();
    }
}
