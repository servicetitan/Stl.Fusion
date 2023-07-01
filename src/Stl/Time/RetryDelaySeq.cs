namespace Stl.Time;

[DataContract, MemoryPackable(GenerateType.VersionTolerant)]
public partial record RetryDelaySeq(
    [property: DataMember, MemoryPackOrder(0)] TimeSpan Min,
    [property: DataMember, MemoryPackOrder(1)] TimeSpan Max,
    [property: DataMember, MemoryPackOrder(2)] double Spread)
    : IEnumerable<TimeSpan>
{
    public const double DefaultMinInSeconds = 0.5;
    public const double DefaultMaxInSeconds = 5;
    public const double DefaultSpread = 0.1;
    public const double DefaultMultiplier = 1.41421356237; // Math.Sqrt(2)
    public static readonly TimeSpan DefaultMin = TimeSpan.FromSeconds(DefaultMinInSeconds);
    public static readonly TimeSpan DefaultMax = TimeSpan.FromMinutes(DefaultMaxInSeconds);

    public static RetryDelaySeq Linear(double delayInSeconds, double maxDelta = DefaultSpread)
        => Linear(TimeSpan.FromSeconds(delayInSeconds), maxDelta);
    public static RetryDelaySeq Linear(TimeSpan delay, double maxDelta = DefaultSpread)
        => new(delay, delay, maxDelta, 1);

    public static RetryDelaySeq Exp(double minInSeconds, double maxInSeconds, double spread = DefaultSpread, double multiplier = DefaultMultiplier)
        => new (TimeSpan.FromSeconds(minInSeconds), TimeSpan.FromSeconds(maxInSeconds), spread, multiplier);
    public static RetryDelaySeq Exp(TimeSpan min, TimeSpan max, double spread = DefaultSpread, double multiplier = DefaultMultiplier)
        => new (min, max, spread, multiplier);

    [DataMember, MemoryPackOrder(3)]
    public double Multiplier { get; init; } = DefaultMultiplier;

    public virtual TimeSpan this[int failedTryCount] {
        get {
            if (failedTryCount <= 0)
                return TimeSpan.Zero;

            var multiplier = Math.Pow(Multiplier, failedTryCount - 1);
            var result = (Min.TotalSeconds * multiplier).Clamp(Min.TotalSeconds, Max.TotalSeconds);
            return TimeSpan.FromSeconds(result).ToRandom(Spread).Next();
        }
    }

    public RetryDelaySeq()
        : this(DefaultMin, DefaultMax, DefaultSpread)
    { }

    [JsonConstructor, Newtonsoft.Json.JsonConstructor, MemoryPackConstructor]
    public RetryDelaySeq(
        TimeSpan min, TimeSpan max,
        double spread = DefaultSpread,
        double multiplier = DefaultMultiplier)
        : this(min, max, spread)
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
        => $"[{Min.ToShortString()} ± {Spread:P0} .. {Max.ToShortString()}]";
}
