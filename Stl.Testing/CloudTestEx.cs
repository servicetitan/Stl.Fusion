using System;
using Stl.IO;

namespace Stl.Testing
{
    public static class CloudTestEx
    {
        private static readonly Guid TestRunId = Guid.NewGuid();

        public static string GetTestRunId(string prefix, int maxLength = 12) 
            => PathEx.GetHashedName($"{prefix}_{TestRunId}", prefix, maxLength);

        public static string GetAzureTestRunId(string prefix, int maxLength = 12)
            => GetTestRunId(prefix, maxLength).Replace("_", "-").ToLowerInvariant();
    }
}
