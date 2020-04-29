using System;
using System.Runtime.InteropServices;

namespace Stl.OS
{
    public enum OSKind
    {
        Unix = 0,
        Windows = 1,
        MacOS = 2,
    }

    public static class OSInfo
    {
        public static readonly OSKind Kind;
        public static readonly string UserHomePath;

        static OSInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Kind = OSKind.Windows;
                UserHomePath = Environment.GetEnvironmentVariable("USERPROFILE") ?? "";
            }
            else {
                Kind = RuntimeInformation.IsOSPlatform(OSPlatform.OSX) 
                    ? OSKind.MacOS 
                    : OSKind.Unix;
                UserHomePath = Environment.GetEnvironmentVariable("HOME") ?? "";
            }
        }
    }
}
