using System.Collections.Generic;
using System.IO;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.IO
{
    public static class PathStringEx
    {
        public static FileInfo GetFileInfo(this PathString path) 
            => new FileInfo(path.Value);
        
        public static DirectoryInfo GetDirectoryInfo(this PathString path) 
            => new DirectoryInfo(path.Value);

        public static Task<string> ReadTextAsync(
            this PathString path, 
            Encoding? encoding = null, 
            CancellationToken cancellationToken = default) 
            => File.ReadAllTextAsync(path, encoding ?? Encoding.UTF8, cancellationToken);

        public static async IAsyncEnumerable<string> ReadLinesAsync(
            this PathString path, 
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            using var reader = File.OpenText(path);
            while (!reader.EndOfStream) {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await reader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                    break;
                yield return line;
            }
        }
    }
}
