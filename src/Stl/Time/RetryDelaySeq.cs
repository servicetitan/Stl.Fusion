namespace Stl.Time;

[DataContract]
public record RetryDelaySeq(
    [property: DataMember] TimeSpan Min,
    [property: DataMember] TimeSpan Max,
    [property: DataMember] double MaxDelta = 0.1)
    : IEnumerable<TimeSpan>
{
    private static readonly TimeSpan DefaultMin = TimeSpan.FromSeconds(0.5);
    private static readonly TimeSpan DefaultMax = TimeSpan.FromMinutes(5);

    [DataMember] public double Multiplier { get; init; } = Math.Sqrt(2);

    public virtual TimeSpan this[int failedTryCount] {
        get {
            if (failedTryCount <= 0)
                return TimeSpan.Zero;
            var multiplier = Math.Pow(Multiplier, failedTryCount - 1);
            var result = (Min.TotalSeconds * multiplier).Clamp(Min.TotalSeconds, Max.TotalSeconds);
            return TimeSpan.FromSeconds(result).ToRandom(MaxDelta).Next();
        }
    }

    public RetryDelaySeq() : this(DefaultMin) { }
    public RetryDelaySeq(TimeSpan min) : this(min, DefaultMax) { }
    public RetryDelaySeq(double minInSeconds) 
        : this(TimeSpan.FromSeconds(minInSeconds), DefaultMax) { }
    public RetryDelaySeq(double minInSeconds, double maxInSeconds, double maxDelta = 0.1) 
        : this(TimeSpan.FromSeconds(minInSeconds), TimeSpan.FromSeconds(maxInSeconds), maxDelta) { }

    IEnumerator IEnumerable.GetEnumerator() => GetEnumerator();
    public IEnumerator<TimeSpan> GetEnumerator()
    {
        for (var retryCount = 0;; retryCount++)
            yield return this[retryCount];
        // ReSharper disable once IteratorNeverReturns
    }

    // Conversion

    public override string ToString() 
        => $"[{Min.ToShortString()} ± {MaxDelta * 100:P} .. {Max.ToShortString()}]";

    public static implicit operator RetryDelaySeq(TimeSpan min) => new(min);
    public static implicit operator RetryDelaySeq(double minInSeconds) => new(TimeSpan.FromSeconds(minInSeconds));
    public static implicit operator RetryDelaySeq((TimeSpan Min, TimeSpan Max) pair) => new(pair.Min, pair.Max);
    public static implicit operator RetryDelaySeq((double MinInSeconds, double MaxInSeconds) pair) => new(pair.MinInSeconds, pair.MaxInSeconds); 
}
