using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.ExceptionServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.IO;
using Stl.Time;

namespace Stl.Locking
{
    public class FileLock : IAsyncLock
    {
        public static readonly IEnumerable<TimeSpan> DefaultRetryIntervals =
            Intervals.Exponential(TimeSpan.FromMilliseconds(50), 1.25, TimeSpan.FromSeconds(1));

        public ReentryMode ReentryMode => ReentryMode.UncheckedDeadlock;
        public PathString Path { get; }
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

        public FileLock(PathString path, IEnumerable<TimeSpan>? retryIntervals = null)
        {
            Path = path;
            RetryIntervals = retryIntervals ?? DefaultRetryIntervals;
        }

        public async ValueTask<IDisposable> Lock(CancellationToken cancellationToken = default)
        {
            try {
                if (!File.Exists(Path))
                    await File.WriteAllTextAsync(Path, "", cancellationToken)
                        .ConfigureAwait(false);
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
                    ExceptionDispatchInfo.Throw(error!);
                await Task.Delay(retryInterval.Current, cancellationToken)
                    .ConfigureAwait(false);
            }
            return fs;
        }

        public static ValueTask<IDisposable> Lock(PathString path, CancellationToken cancellationToken = default)
            => Lock(path, null, cancellationToken);
        public static ValueTask<IDisposable> Lock(PathString path, IEnumerable<TimeSpan>? retryIntervals = null, CancellationToken cancellationToken = default)
        {
            var fileLock = new FileLock(path, retryIntervals);
            return fileLock.Lock(cancellationToken);
        }
    }
}
