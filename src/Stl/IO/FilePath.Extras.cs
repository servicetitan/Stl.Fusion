using System.Text.RegularExpressions;

namespace Stl.IO;

public readonly partial struct FilePath
{
    private static readonly Regex NonAlphaOrNumberRe =
        new("[^a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex LeadingUnderscoresRe =
        new("^_+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex TrailingUnderscoresRe =
        new("_+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

    public static FilePath GetHashedName(
        string key, string? prefix = null,
        int maxLength = 40, bool alwaysHash = false)
    {
        if (maxLength < 8 || maxLength > 128)
            throw new ArgumentOutOfRangeException(nameof(maxLength));

        var result = prefix ?? key;
        result = NonAlphaOrNumberRe.Replace(result, "_");
        result = TrailingUnderscoresRe.Replace(result, "");

        var mustAddHash = alwaysHash || !StringComparer.Ordinal.Equals(result, key);
        if (mustAddHash || result.Length > maxLength) {
            var hash = Convert.ToBase64String(BitConverter.GetBytes(key.GetDeterministicHashCode()));
            hash = NonAlphaOrNumberRe.Replace(hash, "_");
            hash = LeadingUnderscoresRe.Replace(hash, "");
            hash = TrailingUnderscoresRe.Replace(hash, "");
            var prefixLength = Math.Min(result.Length, maxLength - hash.Length - 1);
            result = $"{result.Substring(0, prefixLength)}_{hash}";
        }

        return result;
    }

    public static FilePath GetApplicationDirectory()
    {
        var assembly = Assembly.GetEntryAssembly();
        if (assembly?.GetName()?.Name?.StartsWith("testhost", StringComparison.Ordinal) ?? false) // Unit tests
            assembly = Assembly.GetExecutingAssembly();
        return Path.GetDirectoryName(assembly?.Location) ?? Environment.CurrentDirectory;
    }

    public static FilePath GetApplicationTempDirectory(string appId = "", bool createIfAbsents = false)
    {
        if (string.IsNullOrEmpty(appId))
            appId = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "unknown";
        var subdirectory = GetHashedName($"{appId}_{GetApplicationDirectory()}");
        var path = Path.GetTempPath() & subdirectory;
        if (!Directory.Exists(path))
            Directory.CreateDirectory(path);
        return path;
    }
}
