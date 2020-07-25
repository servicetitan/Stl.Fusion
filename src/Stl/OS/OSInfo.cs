using System;
using System.Runtime.InteropServices;

namespace Stl.OS
{
    public enum OSKind
    {
        Unix = 0,
        Windows = 1,
        MacOS = 2,
        WebAssembly = 3,
    }

    public static class OSInfo
    {
        public static readonly OSKind Kind;
        public static readonly string UserHomePath;

        static OSInfo()
        {
            // WebAssembly
            if (RuntimeInformation.OSDescription == "web" && RuntimeInformation.FrameworkDescription.StartsWith("Mono")) {
                Kind = OSKind.WebAssembly;
                UserHomePath = "";
                return;
            }

            // Windows
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Kind = OSKind.Windows;
                UserHomePath = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
                return;
            }

            // Unix
            Kind = RuntimeInformation.IsOSPlatform(OSPlatform.OSX)
                ? OSKind.MacOS
                : OSKind.Unix;
            UserHomePath = Environment.GetEnvironmentVariable("HOME") ?? "";
        }
    }
}
