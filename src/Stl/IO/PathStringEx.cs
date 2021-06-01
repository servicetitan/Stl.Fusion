using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.IO
{
    public static class PathStringEx
    {
        // GetXxxInfo

        public static FileInfo GetFileInfo(this PathString path)
            => new FileInfo(path.Value);
        public static DirectoryInfo GetDirectoryInfo(this PathString path)
            => new DirectoryInfo(path.Value);

        // EnumerateXxx

        public static IEnumerable<PathString> EnumerateFiles(this PathString path)
            => Directory.EnumerateFiles(path).Select(PathString.New);
        public static IEnumerable<PathString> EnumerateFiles(this PathString path, string searchPattern)
            => Directory.EnumerateFiles(path, searchPattern).Select(PathString.New);
#if !NETSTANDARD2_0
        public static IEnumerable<PathString> EnumerateFiles(this PathString path, string searchPattern, EnumerationOptions enumerationOptions)
            => Directory.EnumerateFiles(path, searchPattern, enumerationOptions).Select(PathString.New);
#endif
        public static IEnumerable<PathString> EnumerateFiles(this PathString path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateFiles(path, searchPattern, searchOption).Select(PathString.New);

        public static IEnumerable<PathString> EnumerateDirectories(this PathString path)
            => Directory.EnumerateDirectories(path).Select(PathString.New);
        public static IEnumerable<PathString> EnumerateDirectories(this PathString path, string searchPattern)
            => Directory.EnumerateDirectories(path, searchPattern).Select(PathString.New);
#if !NETSTANDARD2_0
        public static IEnumerable<PathString> EnumerateDirectories(this PathString path, string searchPattern, EnumerationOptions enumerationOptions)
            => Directory.EnumerateDirectories(path, searchPattern, enumerationOptions).Select(PathString.New);
#endif
        public static IEnumerable<PathString> EnumerateDirectories(this PathString path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateDirectories(path, searchPattern, searchOption).Select(PathString.New);

        public static IEnumerable<PathString> EnumerateFileSystemEntries(this PathString path)
            => Directory.EnumerateFileSystemEntries(path).Select(PathString.New);
        public static IEnumerable<PathString> EnumerateFileSystemEntries(this PathString path, string searchPattern)
            => Directory.EnumerateFileSystemEntries(path, searchPattern).Select(PathString.New);
#if !NETSTANDARD2_0
        public static IEnumerable<PathString> EnumerateFileSystemEntries(this PathString path, string searchPattern, EnumerationOptions enumerationOptions)
            => Directory.EnumerateFileSystemEntries(path, searchPattern, enumerationOptions).Select(PathString.New);
#endif
        public static IEnumerable<PathString> EnumerateFileSystemEntries(this PathString path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption).Select(PathString.New);

        // ReadXxx

        public static Task<string> ReadText(
            this PathString path,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
            => FileCompatEx.ReadAllTextAsync(path, encoding ?? Encoding.UTF8, cancellationToken);

        public static async IAsyncEnumerable<string> ReadLines(
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
