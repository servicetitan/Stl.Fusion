using System;
using System.IO;
using System.Reflection;
using System.Text.RegularExpressions;

namespace Stl.IO
{
    public static class PathEx
    {
        private static readonly Regex NonAlphaOrNumberRegex = 
            new Regex("[^a-z0-9_]+", RegexOptions.Compiled | RegexOptions.IgnoreCase);

        public static string GetHashedName(string? key, string? prefix = null, int maxLength = 40)
        {
            if (maxLength < 16 || maxLength > 128)
                throw new ArgumentOutOfRangeException(nameof(maxLength));
            if (key == null)
                return "(null)";
            prefix ??= key;
            var hash = Convert.ToBase64String(BitConverter.GetBytes(key.GetDeterministicHashCode()));
            var maxPrefixLength = maxLength - hash.Length - 1;
            if (prefix.Length > maxLength)
                prefix = prefix.Substring(0, maxPrefixLength);
            var name = $"{prefix}_{hash}";
            name = NonAlphaOrNumberRegex.Replace(name, "_");
            return name;
        }

        public static string GetApplicationDirectory()
        {
            var assembly = Assembly.GetEntryAssembly();
            if (assembly?.GetName()?.Name?.StartsWith("testhost") ?? false) // Unit tests
                assembly = Assembly.GetExecutingAssembly();
            return Path.GetDirectoryName(assembly?.Location) ?? Environment.CurrentDirectory;
        }

        public static string GetApplicationTempDirectory(string appId = "")
        {
            if (string.IsNullOrEmpty(appId))
                appId = Assembly.GetEntryAssembly()?.GetName()?.Name ?? "unknown";
            var subdirectory = GetHashedName($"{appId}_{GetApplicationDirectory()}");
            return Path.Combine(Path.GetTempPath(), subdirectory);
        }
    }
}
