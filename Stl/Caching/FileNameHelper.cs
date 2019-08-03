using System;
using System.Text.RegularExpressions;

namespace Stl.Caching
{
    public static class FileNameHelper
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
            var hash = Convert.ToBase64String(BitConverter.GetBytes(key.GetHashCode()));
            var name = prefix.Substring(0, maxLength - hash.Length - 1) + "_" + hash;
            name = NonAlphaOrNumberRegex.Replace(name, "_");
            return name;
        }
    }
}
