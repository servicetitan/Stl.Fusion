using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.IO
{
    public class FileLock
    {
        public static readonly TimeSpan DefaultRetryPeriod = TimeSpan.FromMilliseconds(100);
        
        public PathString Path { get; }
        public TimeSpan RetryPeriod { get; }

        public FileLock(PathString path, TimeSpan? retryPeriod = null)
        {
            Path = path;
            RetryPeriod = retryPeriod ?? DefaultRetryPeriod;
        }

        public async Task<IAsyncDisposable> AcquireAsync(CancellationToken cancellationToken = default)
        {
            try {
                if (!File.Exists(Path))
                    await File.WriteAllTextAsync(Path, "", cancellationToken)
                        .ConfigureAwait(false);
            }
            catch (IOException) {}
            catch (UnauthorizedAccessException) {}
            var fs = (FileStream?) null;
            while (fs == null) {
                try {
                    fs = File.OpenWrite(Path);
                }
                catch (IOException) {}
                catch (UnauthorizedAccessException) {}
                await Task.Delay(RetryPeriod, cancellationToken)
                    .ConfigureAwait(false);
            }
            return fs;
        }
    }
}