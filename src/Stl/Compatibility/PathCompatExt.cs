#if NETSTANDARD2_0

#pragma warning disable MA0006

// ReSharper disable once CheckNamespace
namespace System.IO;

public static class PathCompatExt
{
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool IsPathFullyQualified(string path)
        => !PartiallyQualified(path);

    public static string GetRelativePath(string relativeTo, string path)
    {
        // From https://stackoverflow.com/questions/275689/how-to-get-relative-path-from-absolute-path

        if (relativeTo.IsNullOrEmpty()) throw new ArgumentNullException(nameof(relativeTo));
        if (path.IsNullOrEmpty())   throw new ArgumentNullException(nameof(path));

        Uri fromUri = new(relativeTo);
        Uri toUri = new(path);

        if (fromUri.Scheme != toUri.Scheme) { return path; } // path can't be made relative.

        Uri relativeUri = fromUri.MakeRelativeUri(toUri);
        string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

        if (toUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
            relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);

        return relativePath;
    }

    public static string Join(string path1, string path2)
    {
        if (path1.Length == 0)
            return path2;
        return path2.Length == 0 ? path1 : Path.Combine(path1, path2);
    }

    public static string GetFullPath(string path, string basePath)
    {
        // From https://stackoverflow.com/questions/3139467/how-to-get-absolute-file-path-from-base-path-and-relative-containing

        if (path == null)
            throw new ArgumentNullException(nameof(path));
        if (basePath == null)
            throw new ArgumentNullException(nameof(basePath));

        var newPath = Path.Combine(basePath, path);
        var absolute = Path.GetFullPath(newPath);
        return absolute;
    }

    // Private methods

    private static bool PartiallyQualified(string path)
    {
        if (path.Length < 2)
            return true;

        return IsDirectorySeparator(path[0])
            ? path[1] != '?' && !IsDirectorySeparator(path[1])
            : path.Length < 3 || path[1] != ':' || !IsDirectorySeparator(path[2]) || !IsValidDriveChar(path[0]);
    }

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    private static bool IsDirectorySeparator(char c) => c == '\\' || c == '/';

    private static bool IsValidDriveChar(char value)
    {
        if (value is >= 'A' and <= 'Z')
            return true;

        return value is >= 'a' and <= 'z';
    }
}

#endif
