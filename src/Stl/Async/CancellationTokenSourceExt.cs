namespace Stl.Async;

public static class CancellationTokenSourceExt
{
    private static readonly Func<CancellationTokenSource, bool> IsDisposedGetter = typeof(CancellationTokenSource)
        .GetField("_disposed", BindingFlags.Instance | BindingFlags.NonPublic)!
        .GetGetter<CancellationTokenSource, bool>();

    public static void CancelAndDisposeSilently(this CancellationTokenSource? cancellationTokenSource)
    {
        if (cancellationTokenSource == null)
            return;
        try {
            if (cancellationTokenSource.IsCancellationRequested || IsDisposedGetter(cancellationTokenSource))
                return;
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
