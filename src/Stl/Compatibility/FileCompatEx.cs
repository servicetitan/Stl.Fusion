using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Stl.IO;

namespace System.IO
{
    internal static class FileCompatEx
    {
        private static Encoding? s_UTF8NoBOM;
        private static Encoding UTF8NoBOM => s_UTF8NoBOM ?? (s_UTF8NoBOM = new UTF8Encoding(false, true));
        
        public static Task WriteAllTextAsync(string path, string? contents, CancellationToken cancellationToken = default)
            => WriteAllTextAsync(path, contents, UTF8NoBOM, cancellationToken);

        public static Task WriteAllTextAsync(
            string path,
            string? contents,
            Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof (path));
            if (encoding == null)
                throw new ArgumentNullException(nameof (encoding));
            if (path.Length == 0)
                throw new ArgumentException("EmptyPath", nameof (path));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled(cancellationToken);
            if (!string.IsNullOrEmpty(contents)) {
                return Task.Run(() => {
                    File.WriteAllText(path, contents, encoding);
                }, cancellationToken);
            }
            new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read).Dispose();
            return Task.CompletedTask;
        }
        
        public static Task<string> ReadAllTextAsync(string path, CancellationToken cancellationToken = default)
            => ReadAllTextAsync(path, Encoding.UTF8, cancellationToken);

        public static Task<string> ReadAllTextAsync(
            string path,
            Encoding encoding,
            CancellationToken cancellationToken = default)
        {
            if (path == null)
                throw new ArgumentNullException(nameof (path));
            if (encoding == null)
                throw new ArgumentNullException(nameof (encoding));
            if (path.Length == 0)
                throw new ArgumentException("EmptyPath", nameof (path));
            if (cancellationToken.IsCancellationRequested)
                return Task.FromCanceled<string>(cancellationToken);
            var text = File.ReadAllText(path, encoding);
            return Task.FromResult(text);
        }
    }
}
