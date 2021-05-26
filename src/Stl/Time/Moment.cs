using System;
using System.ComponentModel;
using System.Globalization;
using System.Runtime.CompilerServices;
using Newtonsoft.Json;
using Stl.Time.Internal;

namespace Stl.Time
{
    [Serializable]
    [JsonConverter(typeof(MomentJsonConverter))]
    [TypeConverter(typeof(MomentTypeConverter))]
    public readonly struct Moment : IEquatable<Moment>, IComparable<Moment>
    {
        public static readonly Moment MinValue = new(long.MinValue);
        public static readonly Moment MaxValue = new(long.MaxValue);
        public static readonly Moment EpochStart = new(0); // AKA Unix Epoch

        // AKA Unix Time
        public long EpochOffsetTicks { get; }
        public TimeSpan EpochOffset => new TimeSpan(EpochOffsetTicks);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Moment(long epochOffsetTicks)
            => EpochOffsetTicks = epochOffsetTicks;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Moment(TimeSpan epochOffset)
            => EpochOffsetTicks = epochOffset.Ticks;

#if !NETSTANDARD2_0

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Moment(DateTime value)
            : this(value.ToUniversalTime() - DateTime.UnixEpoch) { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Moment(DateTimeOffset value)
            : this(value.ToUniversalTime() - DateTimeOffset.UnixEpoch) { }
        
#else
        
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Moment(DateTime value)
            : this(value.ToUniversalTime() - System.DateTimeEx.UnixEpoch) { }
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public Moment(DateTimeOffset value)
            : this(value.ToUniversalTime() - System.DateTimeOffsetEx.UnixEpoch) { }

#endif

        #region Parse functions

        public static Moment Parse(string source) => DateTime.Parse(source, CultureInfo.InvariantCulture);
        
#if !NETSTANDARD2_0
        public static Moment Parse(ReadOnlySpan<char> source) => DateTime.Parse(source, CultureInfo.InvariantCulture);
#endif
        public static bool TryParse(string source, out Moment result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }
        
#if !NETSTANDARD2_0
        public static bool TryParse(ReadOnlySpan<char> source, out Moment result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }
#endif

        #endregion

        // Conversion

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Moment(DateTime source) => new(source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator Moment(DateTimeOffset source) => new(source);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DateTime(Moment source) => source.ToDateTime();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static implicit operator DateTimeOffset(Moment source) => source.ToDateTimeOffset();

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToDateTime() => DateTime.UnixEpoch + EpochOffset;
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToDateTime() => System.DateTimeEx.UnixEpoch + EpochOffset;
#endif

        public DateTime ToDateTime(DateTime min, DateTime max)
            => Clamp(new Moment(min), new Moment(max)).ToDateTime();
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTime ToDateTimeClamped()
            => ToDateTime(DateTime.MinValue.ToUniversalTime(), DateTime.MaxValue.ToUniversalTime());

#if !NETSTANDARD2_0
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ToDateTimeOffset() => DateTimeOffset.UnixEpoch + EpochOffset;
#else
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public DateTimeOffset ToDateTimeOffset() => System.DateTimeOffsetEx.UnixEpoch + EpochOffset;
#endif
        
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
            => ToDateTimeClamped().ToString(CultureInfo.InvariantCulture);
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
        public static Moment operator +(Moment t1, TimeSpan t2) => new Moment(t1.EpochOffsetTicks + t2.Ticks);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Moment operator -(Moment t1, TimeSpan t2) => new Moment(t1.EpochOffsetTicks - t2.Ticks);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan operator -(Moment t1, Moment t2) => new TimeSpan(t1.EpochOffsetTicks - t2.EpochOffsetTicks);

        // Static methods

        public static Moment Min(Moment first, Moment second)
            => new(Math.Min(first.EpochOffsetTicks, second.EpochOffsetTicks));
        public static Moment Max(Moment first, Moment second)
            => new(Math.Max(first.EpochOffsetTicks, second.EpochOffsetTicks));
    }
}
