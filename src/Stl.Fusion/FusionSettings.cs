using Stl.OS;

namespace Stl.Fusion;

public static class FusionSettings
{
    private static readonly object Lock = new();
    private static FusionMode _mode;

    public static FusionMode Mode {
        get => _mode;
        set {
            if (value != FusionMode.Client && value != FusionMode.Server)
                throw new ArgumentOutOfRangeException(nameof(value), value, null);

            lock (Lock) {
                _mode = value;
                var timeoutsConcurrencyLevel = value == FusionMode.Server
                    ? HardwareInfo.GetProcessorCountPo2Factor()
                    : Math.Max(1, HardwareInfo.GetProcessorCountPo2Factor() / 4);
                TimeoutsConcurrencyLevel = timeoutsConcurrencyLevel;
            }
        }
    }

    public static int TimeoutsConcurrencyLevel { get; set; }

    static FusionSettings()
        => Mode = OSInfo.IsAnyClient ? FusionMode.Client : FusionMode.Server;
}
