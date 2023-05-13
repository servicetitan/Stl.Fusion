namespace Stl.Versioning.Providers;

public sealed class ClockBasedVersionGenerator : VersionGenerator<long>
{
    public static VersionGenerator<long> DefaultPrecise { get; set; } = new ClockBasedVersionGenerator(MomentClockSet.Default.SystemClock);
    public static VersionGenerator<long> DefaultCoarse { get; set; } = new ClockBasedVersionGenerator(MomentClockSet.Default.CoarseSystemClock);

    private readonly IMomentClock _clock;

    public ClockBasedVersionGenerator(IMomentClock clock)
        => _clock = clock;

#pragma warning disable MA0061
    public override long NextVersion(long currentVersion = 0)
#pragma warning restore MA0061
    {
        var nextVersion = _clock.Now.EpochOffset.Ticks;
        return nextVersion > currentVersion ? nextVersion : ++currentVersion;
    }
}
