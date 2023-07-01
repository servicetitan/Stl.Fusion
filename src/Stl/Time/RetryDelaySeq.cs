namespace Stl.Time;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record RetryDelaySeq(
    [property: DataMember, MemoryPackOrder(0)] TimeSpan Min,
    [property: DataMember, MemoryPackOrder(1)] TimeSpan Max,
    [property: DataMember, MemoryPackOrder(2)] double MaxDelta = 0.1)
    : IEnumerable<TimeSpan>
{
    private static readonly TimeSpan DefaultMin = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan DefaultMax = TimeSpan.FromMinutes(5);

    public static RetryDelaySeq Linear(double delayInSeconds, double maxDelta = 0.1)
        => Linear(TimeSpan.FromSeconds(delayInSeconds), maxDelta);
    public static RetryDelaySeq Linear(TimeSpan delay, double maxDelta = 0.1)
        => new(delay, delay, maxDelta, 1);

    [DataMember, MemoryPackOrder(3)]
    public double Multiplier { get; init; } = Math.Sqrt(2);

    public virtual TimeSpan this[int failedTryCount] {
        get {
            if (failedTryCount <= 0)
                return TimeSpan.Zero;

            var multiplier = Math.Pow(Multiplier, failedTryCount - 1);
            var result = (Min.TotalSeconds * multiplier).Clamp(Min.TotalSeconds, Max.TotalSeconds);
            return TimeSpan.FromSeconds(result).ToRandom(MaxDelta).Next();
        }
    }

    public RetryDelaySeq() : this(DefaultMin, DefaultMax) { }
    public RetryDelaySeq(TimeSpan min) : this(min, DefaultMax) { }
    public RetryDelaySeq(double minInSeconds)
        : this(TimeSpan.FromSeconds(minInSeconds), DefaultMax) { }
    public RetryDelaySeq(double minInSeconds, double maxInSeconds, double maxDelta = 0.1)
        : this(TimeSpan.FromSeconds(minInSeconds), TimeSpan.FromSeconds(maxInSeconds), maxDelta) { }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public RetryDelaySeq(TimeSpan min, TimeSpan max, double maxDelta, double multiplier)
        : this(min, max, maxDelta)
        => Multiplier = multiplier;

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<TimeSpan> GetEnumerator()
    {
        for (var retryCount = 0;; retryCount++)
            yield return this[retryCount];
        // ReSharper disable once IteratorNeverReturns
    }

    // Conversion

    public override string ToString()
        => $"[{Min.ToShortString()} ± {MaxDelta:P0} .. {Max.ToShortString()}]";

    public static implicit operator RetryDelaySeq(TimeSpan min) => new(min);
    public static implicit operator RetryDelaySeq(double minInSeconds) => new(TimeSpan.FromSeconds(minInSeconds));
    public static implicit operator RetryDelaySeq((TimeSpan Min, TimeSpan Max) pair) => new(pair.Min, pair.Max);
    public static implicit operator RetryDelaySeq((double MinInSeconds, double MaxInSeconds) pair) => new(pair.MinInSeconds, pair.MaxInSeconds);
}
