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
}
