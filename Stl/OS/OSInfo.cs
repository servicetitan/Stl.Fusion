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
        public static OSKind Kind { get; }

        static OSInfo()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) {
                Kind = OSKind.Windows;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX)) {
                Kind = OSKind.MacOS;
            }
            else {
                Kind = OSKind.Unix;
            }
        }
    }


}
