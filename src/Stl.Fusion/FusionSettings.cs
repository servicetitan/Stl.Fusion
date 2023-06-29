using Stl.OS;

namespace Stl.Fusion;

public static class FusionSettings
{
    private static readonly object Lock = new();
    private static FusionMode _mode;
    private static PrimeSieve? _primeSieve;

    public static FusionMode Mode {
        get => _mode;
        set {
            if (value != FusionMode.Client && value != FusionMode.Server)
                throw new ArgumentOutOfRangeException(nameof(value), value, null);

            lock (Lock) {
                _mode = value;
                Recompute();
            }
        }
    }

    public static int TimeoutsConcurrencyLevel { get; set; }
    public static int ComputedRegistryConcurrencyLevel { get; set; }
    public static int ComputedRegistryCapacity { get; set; }
    public static int ComputedGraphPrunerBatchSize { get; set; }

    static FusionSettings()
        => Mode = OSInfo.IsAnyClient ? FusionMode.Client : FusionMode.Server;

    private static void Recompute()
    {
        var isServer = Mode == FusionMode.Server;
        var cpuCountPo2 = HardwareInfo.ProcessorCountPo2;
        TimeoutsConcurrencyLevel = (cpuCountPo2 / (isServer ? 1 : 4)).Clamp(1, isServer ? 1024 : 4);
        ComputedRegistryConcurrencyLevel = cpuCountPo2 * (isServer ? 4 : 1);
        var computedRegistryCapacity = (ComputedRegistryConcurrencyLevel * 32).Clamp(256, 8192);
        var primeSieve = GetPrimeSieve(computedRegistryCapacity + 16);
        while (!primeSieve.IsPrime(computedRegistryCapacity))
            computedRegistryCapacity--;
        ComputedRegistryCapacity = computedRegistryCapacity;
        ComputedGraphPrunerBatchSize = cpuCountPo2 * 512;
    }

    private static PrimeSieve GetPrimeSieve(int limit)
        => _primeSieve?.Limit >= limit
            ? _primeSieve
            : _primeSieve = new PrimeSieve(limit);
}
