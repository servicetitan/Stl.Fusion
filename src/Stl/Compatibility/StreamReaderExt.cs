#if !NET7_0_OR_GREATER

// ReSharper disable once CheckNamespace
namespace System.IO;

public static class StreamReaderExt
{
    public static Task<string?> ReadLineAsync(this StreamReader streamReader, CancellationToken cancellationToken)
        => streamReader.ReadLineAsync();

    public static Task<string> ReadToEndAsync(this StreamReader streamReader, CancellationToken cancellationToken)
        => streamReader.ReadToEndAsync();
}

#endif
