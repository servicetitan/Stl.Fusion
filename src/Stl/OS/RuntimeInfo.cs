using System;
using System.Reflection;

namespace Stl.OS
{
    public static class RuntimeInfo
    {
        public static class DotNetCore
        {
            public static readonly string? VersionString;
            public static readonly Version? Version;

            static DotNetCore()
            {
                var assembly = typeof(System.Runtime.GCSettings).GetTypeInfo().Assembly;
                var assemblyPath = assembly.CodeBase.Split(
                    new[] { '/', '\\' }, StringSplitOptions.RemoveEmptyEntries);
                var netCoreAppIndex = Array.IndexOf(assemblyPath, "Microsoft.NETCore.App");
                if (netCoreAppIndex > 0 && netCoreAppIndex < assemblyPath.Length - 2) {
                    VersionString = assemblyPath[netCoreAppIndex + 1];
                    if (Version.TryParse(VersionString ?? "", out var version))
                        Version = version;
                    else
                        VersionString = null;
                }
            }
        }
    }
}
