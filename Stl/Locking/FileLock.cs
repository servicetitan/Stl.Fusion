using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Stl.Async;
using Stl.Collections;
using Stl.IO;

namespace Stl.Locking
{
    public class FileLock : IAsyncLock
    {
        public static readonly IEnumerable<TimeSpan> DefaultRetryIntervals = 
            Intervals.Exponential(TimeSpan.FromMilliseconds(50), 1.25, TimeSpan.FromSeconds(1));

        public ReentryMode ReentryMode => ReentryMode.UncheckedDeadlock;
        public PathString Path { get; }
        public IEnumerable<TimeSpan> RetryIntervals { get; }

        public FileLock(PathString path, IEnumerable<TimeSpan>? retryIntervals = null)
        {
            Path = path;
            RetryIntervals = retryIntervals ?? DefaultRetryIntervals;
        }

        public ValueTask<bool> IsLockedAsync()
        {
            try {
                using var _ = File.OpenWrite(Path);
                return ValueTaskEx.FalseTask; 
            }
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
            return ValueTaskEx.TrueTask; 
        }

        public bool? IsLockedLocally() => throw new NotSupportedException();

        public async ValueTask<IAsyncDisposable> LockAsync(CancellationToken cancellationToken = default)
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
                try {
                    fs = File.OpenWrite(Path);
                }
                catch (IOException) {}
                catch (UnauthorizedAccessException) {}
                if (fs != null)
                    break;
                retryInterval.MoveNext();
                await Task.Delay(retryInterval.Current, cancellationToken)
                    .ConfigureAwait(false);
            }
            return fs;
        }

        public static ValueTask<IAsyncDisposable> LockAsync(PathString path, CancellationToken cancellationToken = default)
            => LockAsync(path, null, cancellationToken);
        public static ValueTask<IAsyncDisposable> LockAsync(PathString path, IEnumerable<TimeSpan>? retryIntervals = null, CancellationToken cancellationToken = default)
        {
            var fileLock = new FileLock(path, retryIntervals);
            return fileLock.LockAsync(cancellationToken);
        }
    }
}
