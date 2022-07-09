using Stl.Generators;

namespace Stl.Time;

[StructLayout(LayoutKind.Auto)]
[DataContract]
public readonly record struct RandomTimeSpan
{
    [DataMember(Order = 0)] public TimeSpan Origin { get; init; }
    [DataMember(Order = 1)] public TimeSpan MaxDelta { get; init; }
    [JsonIgnore, Newtonsoft.Json.JsonIgnore] TimeSpan Min => (Origin - MaxDelta).Positive(); 
    [JsonIgnore, Newtonsoft.Json.JsonIgnore] TimeSpan Max => (Origin + MaxDelta).Positive(); 

    [JsonConstructor, Newtonsoft.Json.JsonConstructor]
    public RandomTimeSpan(TimeSpan origin, TimeSpan maxDelta = default)
    {
        Origin = origin;
        MaxDelta = maxDelta;
    }

    public RandomTimeSpan(TimeSpan origin, double maxDelta)
    {
        Origin = origin;
        MaxDelta = TimeSpan.FromSeconds(maxDelta * origin.TotalSeconds);
    }

    public RandomTimeSpan(double originInSeconds, double maxDelta = default)
    {
        Origin = TimeSpan.FromSeconds(originInSeconds);
        MaxDelta = TimeSpan.FromSeconds(maxDelta * originInSeconds);
    }

    // Conversion

    public override string ToString()
        => MaxDelta <= TimeSpan.Zero
            ? Origin.ToString()
            : $"({Origin.ToShortString()} ± {MaxDelta.ToShortString()})";

    public AsyncChain ToDelayChain(IMomentClock? clock = null)
        => AsyncChain.Delay(this, clock);

    public static implicit operator RandomTimeSpan(TimeSpan origin) => new(origin);
    public static implicit operator RandomTimeSpan(double originInSeconds) => new(originInSeconds);
    public static implicit operator RandomTimeSpan((TimeSpan Origin, TimeSpan MaxDelta) pair) => new(pair.Origin, pair.MaxDelta);
    public static implicit operator RandomTimeSpan((double OriginInSeconds, double MaxDelta) pair) => new(pair.OriginInSeconds, pair.MaxDelta);

    public TimeSpan Next()
    {
        if (MaxDelta <= TimeSpan.Zero)
            return Origin;
        var deltaSeconds = MaxDelta.TotalSeconds * 2 * (ConcurrentRandomDoubleGenerator.Default.Next() - 0.5);
        return (Origin + TimeSpan.FromSeconds(deltaSeconds)).Positive();
    }
}
