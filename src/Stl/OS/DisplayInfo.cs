using System.Diagnostics;
using System.Drawing;
using System.Globalization;

namespace Stl.OS;

public static class DisplayInfo
{
    public static Rectangle? PrimaryDisplayDimensions { get; }

    static DisplayInfo()
    {
        PrimaryDisplayDimensions = null;
        try {
            switch (OSInfo.Kind) {
            case OSKind.Windows:
                var p = new Process() {
                    StartInfo = new ProcessStartInfo() {
                        FileName = "cmd.exe",
                        Arguments = "/c wmic path Win32_VideoController get CurrentHorizontalResolution,CurrentVerticalResolution",
                        UseShellExecute = false,
                        RedirectStandardOutput = true,
                        CreateNoWindow = true
                    }
                };
                p.Start();
                p.WaitForExit();
                var wh = p.StandardOutput.ReadToEnd().TrimEnd()
                    .Split("\r\n").Last()
                    .Split(" ", StringSplitOptions.RemoveEmptyEntries);
                var w = int.Parse(wh[0], NumberStyles.Integer, CultureInfo.InvariantCulture);
                var h = int.Parse(wh[1], NumberStyles.Integer, CultureInfo.InvariantCulture);
                PrimaryDisplayDimensions = new Rectangle(0, 0, w, h);
                break;
            }
        }
        catch {
            PrimaryDisplayDimensions = null;
        }
    }
}
