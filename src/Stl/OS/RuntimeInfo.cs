using System;
using System.Reflection;
using Stl.Text;

namespace Stl.OS
{
    public static class RuntimeInfo
    {
        public static class Process
        {
            public static readonly Guid Guid = Guid.NewGuid();
            public static readonly Symbol Id =
                Convert.ToBase64String(Guid.ToByteArray()).TrimEnd('=');
            public static readonly Symbol MachinePrefixedId =
                $"{Environment.MachineName}:{Id.Value}";
        }

        public static class DotNetCore
        {
            public static readonly string? VersionString;
            public static readonly Version? Version;

            static DotNetCore()
            {
                var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
                var assemblyPath = assembly.Location.Split(
                    new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
                if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2) {
                    VersionString = assemblyPath[netCoreAppIndex + 1];
                    if (Version.TryParse(VersionString.NullIfEmpty() ?? "", out var version))
                        Version = version;
                }
            }
        }
    }
}
