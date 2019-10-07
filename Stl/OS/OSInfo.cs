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
        public static readonly string HostName;

        static OSInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Kind = OSKind.Windows;
                HostName = Environment.GetEnvironmentVariable("COMPUTERNAME") ?? "";
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                Kind = OSKind.MacOS;
                HostName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "";
            }
            else {
                Kind = OSKind.Unix;
                HostName = Environment.GetEnvironmentVariable("HOSTNAME") ?? "";
            }
        }
    }


}
