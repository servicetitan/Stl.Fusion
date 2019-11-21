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
        private static readonly Regex TrailingUnderscoresRe = 
            new Regex("_+$", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static PathString GetHashedName(string? key, string? prefix = null, int maxLength = 40)
        {
            if (maxLength < 8 || maxLength > 128)
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            if (key == null)
                return "(null)";
            prefix ??= key;
            var hash = Convert.ToBase64String(BitConverter.GetBytes(key.GetDeterministicHashCode()));
            var maxPrefixLength = maxLength - hash.Length - 1;
            if (prefix.Length > maxLength)
                prefix = prefix.Substring(0, maxPrefixLength);
            var name = $"{prefix}_{hash}";
            name = NonAlphaOrNumberRe.Replace(name, "_");
            name = TrailingUnderscoresRe.Replace(name, "");
            return name;
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
