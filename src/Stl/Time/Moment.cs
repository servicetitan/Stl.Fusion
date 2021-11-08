using System.ComponentModel;
using System.Globalization;
using System.Text.Json.Serialization;
using Stl.Time.Internal;

namespace Stl.Time;

[DataContract]
[JsonConverter(typeof(MomentJsonConverter))]
[Newtonsoft.Json.JsonConverter(typeof(MomentNewtonsoftJsonConverter))]
[TypeConverter(typeof(MomentTypeConverter))]
public readonly struct Moment : IEquatable<Moment>, IComparable<Moment>
{
    public static readonly Moment MinValue = new(long.MinValue);
    public static readonly Moment MaxValue = new(long.MaxValue);
    public static readonly Moment EpochStart = new(0); // AKA Unix Epoch

    // AKA Unix Time
    [DataMember(Order = 0)]
    public long EpochOffsetTicks { get; }
    public TimeSpan EpochOffset => new(EpochOffsetTicks);

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Moment(long epochOffsetTicks)
        => EpochOffsetTicks = epochOffsetTicks;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Moment(TimeSpan epochOffset)
        => EpochOffsetTicks = epochOffset.Ticks;

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Moment(DateTime value)
        : this(value.ToUniversalTime() - DateTimeExt.UnixEpoch) { }
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Moment(DateTimeOffset value)
        : this(value.ToUniversalTime() - DateTimeOffsetExt.UnixEpoch) { }

    // (Try)Parse

    public static Moment Parse(string source)
        => DateTime
            .Parse(source, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .DefaultKind(DateTimeKind.Utc);

#if !NETSTANDARD2_0
    public static Moment Parse(ReadOnlySpan<char> source)
        => DateTime
            .Parse(source, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind)
            .DefaultKind(DateTimeKind.Utc);
#endif

    public static bool TryParse(string source, out Moment result)
    {
        var success = DateTime.TryParse(source,
            CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var r);
        result = r.DefaultKind(DateTimeKind.Utc);
        return success;
    }

#if !NETSTANDARD2_0
    public static bool TryParse(ReadOnlySpan<char> source, out Moment result)
    {
        var success = DateTime.TryParse(source,
            CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind, out var r);
        result = r.DefaultKind(DateTimeKind.Utc);
        return success;
    }
#endif

    // Conversion

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Moment(DateTime source) => new(source);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator Moment(DateTimeOffset source) => new(source);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DateTime(Moment source) => source.ToDateTime();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static implicit operator DateTimeOffset(Moment source) => source.ToDateTimeOffset();

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ToDateTime()
        => DateTimeExt.UnixEpoch + EpochOffset;
    public DateTime ToDateTime(DateTime min, DateTime max)
        => Clamp(new Moment(min), new Moment(max)).ToDateTime();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTime ToDateTimeClamped()
        => ToDateTime(DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTimeOffset ToDateTimeOffset()
        => DateTimeOffsetExt.UnixEpoch + EpochOffset;
    public DateTimeOffset ToDateTimeOffset(DateTimeOffset min, DateTimeOffset max)
        => Clamp(new Moment(min), new Moment(max)).ToDateTimeOffset();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public DateTimeOffset ToDateTimeOffsetClamped()
        => ToDateTimeOffset(DateTimeOffset.MinValue.ToUniversalTime(), DateTimeOffset.MaxValue.ToUniversalTime());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public double ToUnixEpoch() => EpochOffset.TotalSeconds;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public long ToIntegerUnixEpoch() => (long) Math.Floor(ToUnixEpoch());

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public Moment Clamp(Moment min, Moment max)
        => new(Math.Max(min.EpochOffsetTicks, Math.Min(max.EpochOffsetTicks, EpochOffsetTicks)));

    public override string ToString()
        => ToDateTimeClamped().ToString("o", CultureInfo.InvariantCulture);
    public string ToString(string format)
        => ToDateTimeClamped().ToString(format, CultureInfo.InvariantCulture);
    public string ToString(string format, CultureInfo cultureInfo)
        => ToDateTimeClamped().ToString(format, cultureInfo);

    // Equality

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public bool Equals(Moment other) => EpochOffsetTicks == other.EpochOffsetTicks;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public int CompareTo(Moment other) => EpochOffsetTicks.CompareTo(other.EpochOffsetTicks);
    public override bool Equals(object? obj) => obj is Moment other && Equals(other);
    public override int GetHashCode() => EpochOffsetTicks.GetHashCode();
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator ==(Moment left, Moment right) => left.Equals(right);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator !=(Moment left, Moment right) => !left.Equals(right);

    // Operations

    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >(Moment t1, Moment t2) => t1.EpochOffsetTicks > t2.EpochOffsetTicks;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator >=(Moment t1, Moment t2) => t1.EpochOffsetTicks >= t2.EpochOffsetTicks;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <(Moment t1, Moment t2) => t1.EpochOffsetTicks < t2.EpochOffsetTicks;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static bool operator <=(Moment t1, Moment t2) => t1.EpochOffsetTicks <= t2.EpochOffsetTicks;
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Moment operator +(Moment t1, TimeSpan t2) => new(t1.EpochOffsetTicks + t2.Ticks);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static Moment operator -(Moment t1, TimeSpan t2) => new(t1.EpochOffsetTicks - t2.Ticks);
    [MethodImpl(MethodImplOptions.AggressiveInlining)]
    public static TimeSpan operator -(Moment t1, Moment t2) => new(t1.EpochOffsetTicks - t2.EpochOffsetTicks);

    // Static methods

    public static Moment Min(Moment first, Moment second)
        => new(Math.Min(first.EpochOffsetTicks, second.EpochOffsetTicks));
    public static Moment Max(Moment first, Moment second)
        => new(Math.Max(first.EpochOffsetTicks, second.EpochOffsetTicks));
}
