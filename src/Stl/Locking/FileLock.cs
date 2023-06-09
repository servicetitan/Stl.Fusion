using System.Runtime.ExceptionServices;
using Stl.IO;
using Stl.Locking.Internal;

namespace Stl.Locking;

public class FileLock : AsyncLock
{
    public static readonly IEnumerable<TimeSpan> DefaultRetryIntervals =
        Intervals.Exponential(TimeSpan.FromMilliseconds(50), 1.25, TimeSpan.FromSeconds(1));

    private FileStream? _fileStream;

    public FilePath Path { get; }
    public IEnumerable<TimeSpan> RetryIntervals { get; }

    public static ValueTask<AsyncLockReleaser> Lock(FilePath path, CancellationToken cancellationToken = default)
        => Lock(path, null, cancellationToken);
    public static ValueTask<AsyncLockReleaser> Lock(FilePath path, IEnumerable<TimeSpan>? retryIntervals = null, CancellationToken cancellationToken = default)
    {
        var fileLock = new FileLock(path, retryIntervals);
        return fileLock.Lock(cancellationToken);
    }

    public FileLock(FilePath path, IEnumerable<TimeSpan>? retryIntervals = null)
    {
        Path = path;
        RetryIntervals = retryIntervals ?? DefaultRetryIntervals;
    }

    public override async ValueTask<AsyncLockReleaser> Lock(CancellationToken cancellationToken = default)
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
        _fileStream = fs;
        return new AsyncLockReleaser(this);
    }

    public override void Release()
    {
        var fs = _fileStream;
        _fileStream = null;
        fs?.Dispose();
    }
}
