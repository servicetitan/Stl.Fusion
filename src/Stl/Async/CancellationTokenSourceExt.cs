namespace Stl.Async;

public static class CancellationTokenSourceExt
{
    public static void CancelAndDisposeSilently(this CancellationTokenSource cancellationTokenSource)
    {
        try {
            if (!cancellationTokenSource.IsCancellationRequested)
                cancellationTokenSource.Cancel();
        }
        catch {
            // Intended
        }
        finally {
            cancellationTokenSource.Dispose();
        }
    }

}
