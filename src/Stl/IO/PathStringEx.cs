using System;
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
            CancellationToken cancellationToken = default)
            => path.ReadText(null, cancellationToken);

        public static Task<string> ReadText(
            this PathString path,
            Encoding? encoding,
            CancellationToken cancellationToken = default)
            => FileEx.ReadText(path, encoding, cancellationToken);

        public static IAsyncEnumerable<string> ReadLines(
            this PathString path,
            CancellationToken cancellationToken = default)
            => path.ReadLines(null, false, cancellationToken);

        public static async IAsyncEnumerable<string> ReadLines(
            this PathString path,
            Encoding? encoding,
            bool detectEncodingFromByteOrderMarks,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            encoding ??= FileEx.DefaultReadEncoding;
            using var reader = File.OpenRead(path);
            using var textReader = new StreamReader(reader, encoding, detectEncodingFromByteOrderMarks);
            while (!textReader.EndOfStream) {
                cancellationToken.ThrowIfCancellationRequested();
                var line = await textReader.ReadLineAsync().ConfigureAwait(false);
                if (line == null)
                    break;
                yield return line;
            }
        }

        // WriteXxx

        public static Task WriteText(
            this PathString path,
            string contents,
            CancellationToken cancellationToken = default)
            => path.WriteText(contents, null, cancellationToken);

        public static Task WriteText(
            this PathString path,
            string contents,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
            => FileEx.WriteText(path, contents, encoding, cancellationToken);

        public static Task WriteLines(
            this PathString path,
            IAsyncEnumerable<string> lines,
            CancellationToken cancellationToken = default)
            => path.WriteLines(lines, null, cancellationToken);

        public static async Task WriteLines(
            this PathString path,
            IAsyncEnumerable<string> lines,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            encoding ??= FileEx.DefaultWriteEncoding;
            using var writer = File.OpenWrite(path);
            using var textWriter = new StreamWriter(writer, encoding);
            await foreach (var line in lines.WithCancellation(cancellationToken)) {
                if (line == null)
                    continue;
                await textWriter.WriteLineAsync(line);
            }
        }
    }
}
