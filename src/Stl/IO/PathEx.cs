using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;
using Stl.Text;

namespace Stl.IO
{
    public static class PathEx
    {
        private static readonly Regex NonAlphaOrNumberRe =
            new Regex("[^a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex LeadingUnderscoresRe =
            new Regex("^_+", RegexOptions.Compiled | RegexOptions.IgnoreCase);
        private static readonly Regex TrailingUnderscoresRe =
            new Regex("_+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static PathString GetHashedName(
            string key, string? prefix = null,
            int maxLength = 40, bool alwaysHash = false)
        {
            if (maxLength < 8 || maxLength > 128)
                throw new ArgumentOutOfRangeException(nameof(maxLength));

            var result = prefix ?? key;
            result = NonAlphaOrNumberRe.Replace(result, "_");
            result = TrailingUnderscoresRe.Replace(result, "");

            var mustAddHash = alwaysHash || result != key;
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

        public static PathString GetApplicationDirectory()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly?.GetName()?.Name?.StartsWith("testhost") ?? false) // Unit tests
                assembly = Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(assembly?.Location) ?? Environment.CurrentDirectory;
        }

        public static PathString GetApplicationTempDirectory(string appId = "", bool createIfAbsents = false)
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
}
