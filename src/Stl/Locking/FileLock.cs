using System.Runtime.ExceptionServices;
using Stl.IO;

namespace Stl.Locking;

public class FileLock : IAsyncLock
{
    public static readonly IEnumerable<TimeSpan> DefaultRetryIntervals =
        Intervals.Exponential(TimeSpan.FromMilliseconds(50), 1.25, TimeSpan.FromSeconds(1));

    public ReentryMode ReentryMode => ReentryMode.UncheckedDeadlock;
    public FilePath Path { get; }
    public IEnumerable<TimeSpan> RetryIntervals { get; }
    public bool IsLocked {
        get {
            try {
                using var _ = File.OpenWrite(Path);
                return false;
            }
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
            return true;
        }
    }
    public bool? IsLockedLocally => false;

    public FileLock(FilePath path, IEnumerable<TimeSpan>? retryIntervals = null)
    {
        Path = path;
        RetryIntervals = retryIntervals ?? DefaultRetryIntervals;
    }

    public async ValueTask<IDisposable> Lock(CancellationToken cancellationToken = default)
    {
        try {
            if (!File.Exists(Path))
                await FileExt.WriteText(Path, "", cancellationToken).ConfigureAwait(false);
        }
        catch (IOException) {}
        catch (UnauthorizedAccessException) {}

        // The warning suppressed below is clearly a false positive.
        // ReSharper disable once GenericEnumeratorNotDisposed
        using var retryInterval = RetryIntervals.GetEnumerator();
        var fs = (FileStream?) null;
        while (true) {
            var error = (Exception?) null;
            try {
                fs = File.OpenWrite(Path);
            }
            catch (IOException e) {
                error = e;
            }
            catch (UnauthorizedAccessException e) {
                error = e;
            }
            if (fs != null)
                break;
            if (!retryInterval.MoveNext())
#if !NETSTANDARD2_0
                ExceptionDispatchInfo.Throw(error!);
#else
                ExceptionDispatchInfo.Capture(error!).Throw();
#endif
            await Task.Delay(retryInterval.Current, cancellationToken)
                .ConfigureAwait(false);
        }
        return fs;
    }

    public static ValueTask<IDisposable> Lock(FilePath path, CancellationToken cancellationToken = default)
        => Lock(path, null, cancellationToken);
    public static ValueTask<IDisposable> Lock(FilePath path, IEnumerable<TimeSpan>? retryIntervals = null, CancellationToken cancellationToken = default)
    {
        var fileLock = new FileLock(path, retryIntervals);
        return fileLock.Lock(cancellationToken);
    }
}
