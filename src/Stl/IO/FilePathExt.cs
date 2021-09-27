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
    public static class FilePathExt
    {
        // GetXxxInfo

        public static FileInfo GetFileInfo(this FilePath path)
            => new(path.Value);
        public static DirectoryInfo GetDirectoryInfo(this FilePath path)
            => new(path.Value);

        // EnumerateXxx

        public static IEnumerable<FilePath> EnumerateFiles(this FilePath path)
            => Directory.EnumerateFiles(path).Select(FilePath.New);
        public static IEnumerable<FilePath> EnumerateFiles(this FilePath path, string searchPattern)
            => Directory.EnumerateFiles(path, searchPattern).Select(FilePath.New);
#if !NETSTANDARD2_0
        public static IEnumerable<FilePath> EnumerateFiles(this FilePath path, string searchPattern, EnumerationOptions enumerationOptions)
            => Directory.EnumerateFiles(path, searchPattern, enumerationOptions).Select(FilePath.New);
#endif
        public static IEnumerable<FilePath> EnumerateFiles(this FilePath path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateFiles(path, searchPattern, searchOption).Select(FilePath.New);

        public static IEnumerable<FilePath> EnumerateDirectories(this FilePath path)
            => Directory.EnumerateDirectories(path).Select(FilePath.New);
        public static IEnumerable<FilePath> EnumerateDirectories(this FilePath path, string searchPattern)
            => Directory.EnumerateDirectories(path, searchPattern).Select(FilePath.New);
#if !NETSTANDARD2_0
        public static IEnumerable<FilePath> EnumerateDirectories(this FilePath path, string searchPattern, EnumerationOptions enumerationOptions)
            => Directory.EnumerateDirectories(path, searchPattern, enumerationOptions).Select(FilePath.New);
#endif
        public static IEnumerable<FilePath> EnumerateDirectories(this FilePath path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateDirectories(path, searchPattern, searchOption).Select(FilePath.New);

        public static IEnumerable<FilePath> EnumerateFileSystemEntries(this FilePath path)
            => Directory.EnumerateFileSystemEntries(path).Select(FilePath.New);
        public static IEnumerable<FilePath> EnumerateFileSystemEntries(this FilePath path, string searchPattern)
            => Directory.EnumerateFileSystemEntries(path, searchPattern).Select(FilePath.New);
#if !NETSTANDARD2_0
        public static IEnumerable<FilePath> EnumerateFileSystemEntries(this FilePath path, string searchPattern, EnumerationOptions enumerationOptions)
            => Directory.EnumerateFileSystemEntries(path, searchPattern, enumerationOptions).Select(FilePath.New);
#endif
        public static IEnumerable<FilePath> EnumerateFileSystemEntries(this FilePath path, string searchPattern, SearchOption searchOption)
            => Directory.EnumerateFileSystemEntries(path, searchPattern, searchOption).Select(FilePath.New);

        // ReadXxx

        public static Task<string> ReadText(
            this FilePath path,
            CancellationToken cancellationToken = default)
            => path.ReadText(null, cancellationToken);

        public static Task<string> ReadText(
            this FilePath path,
            Encoding? encoding,
            CancellationToken cancellationToken = default)
            => FileExt.ReadText(path, encoding, cancellationToken);

        public static IAsyncEnumerable<string> ReadLines(
            this FilePath path,
            CancellationToken cancellationToken = default)
            => path.ReadLines(null, false, cancellationToken);

        public static async IAsyncEnumerable<string> ReadLines(
            this FilePath path,
            Encoding? encoding,
            bool detectEncodingFromByteOrderMarks,
            [EnumeratorCancellation] CancellationToken cancellationToken = default)
        {
            encoding ??= FileExt.DefaultReadEncoding;
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
            this FilePath path,
            string contents,
            CancellationToken cancellationToken = default)
            => path.WriteText(contents, null, cancellationToken);

        public static Task WriteText(
            this FilePath path,
            string contents,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
            => FileExt.WriteText(path, contents, encoding, cancellationToken);

        public static Task WriteLines(
            this FilePath path,
            IAsyncEnumerable<string> lines,
            CancellationToken cancellationToken = default)
            => path.WriteLines(lines, null, cancellationToken);

        public static async Task WriteLines(
            this FilePath path,
            IAsyncEnumerable<string> lines,
            Encoding? encoding = null,
            CancellationToken cancellationToken = default)
        {
            encoding ??= FileExt.DefaultWriteEncoding;
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
