using System.Text;

namespace Stl.IO;

public static class FileExt
{
    public static Encoding DefaultReadEncoding { get; } = Encoding.UTF8;
    public static Encoding DefaultWriteEncoding { get; } = new UTF8Encoding(false, true);

    public static Task WriteText(string path, string? contents, CancellationToken cancellationToken = default)
        => WriteText(path, contents, null, cancellationToken);

    public static Task WriteText(
        string path,
        string? contents,
        Encoding? encoding,
        CancellationToken cancellationToken = default)
    {
        encoding ??= DefaultWriteEncoding;
#if !NETSTANDARD2_0
        return File.WriteAllTextAsync(path, contents, encoding, cancellationToken);
#else
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.Length == 0)
            throw new ArgumentException("EmptyPath", nameof(path));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled(cancellationToken);
        if (!string.IsNullOrEmpty(contents)) {
            return Task.Run(() => {
                File.WriteAllText(path, contents, encoding);
            }, cancellationToken);
        }
        new FileStream(path, FileMode.Create, FileAccess.Write, FileShare.Read).Dispose();
        return Task.CompletedTask;
#endif
    }

    public static Task<string> ReadText(string path, CancellationToken cancellationToken = default)
        => ReadText(path, null, cancellationToken);

    public static Task<string> ReadText(
        string path,
        Encoding? encoding,
        CancellationToken cancellationToken = default)
    {
        encoding ??= DefaultReadEncoding;
#if !NETSTANDARD2_0
        return File.ReadAllTextAsync(path, encoding, cancellationToken);
#else
        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (path.Length == 0)
            throw new ArgumentException("EmptyPath", nameof(path));
        if (cancellationToken.IsCancellationRequested)
            return Task.FromCanceled<string>(cancellationToken);
        var text = File.ReadAllText(path, encoding);
        return Task.FromResult(text);
#endif
    }
}
