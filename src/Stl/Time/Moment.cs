using System;
using System.ComponentModel;
using System.Globalization;
using Newtonsoft.Json;
using Stl.Time.Internal;

namespace Stl.Time
{
    [Serializable]
    [JsonConverter(typeof(MomentJsonConverter))]
    [TypeConverter(typeof(MomentTypeConverter))]
    public readonly struct Moment : IEquatable<Moment>, IComparable<Moment>
    {
        public static readonly Moment MaxValue = new Moment(TimeSpan.MaxValue); 
        public static readonly Moment MinValue = new Moment(TimeSpan.MinValue); 
        public static readonly Moment EpochStart = new Moment(TimeSpan.Zero); // AKA Unix Epoch 

        // AKA Unix Time
        public TimeSpan EpochOffset { get; }

        public Moment(TimeSpan epochOffset) 
            => EpochOffset = epochOffset;
        public Moment(DateTime value) 
            => EpochOffset = value.ToUniversalTime() - DateTime.UnixEpoch;
        public Moment(DateTimeOffset value) 
            => EpochOffset = value.ToUniversalTime() - DateTimeOffset.UnixEpoch;
        public Moment(IntMoment value) 
            => EpochOffset = value.EpochOffset + IntMoment.Clock.EpochStartMoment.EpochOffset;

        #region Parse functions
        
        public static Moment Parse(string source) => DateTime.Parse(source, CultureInfo.InvariantCulture);
        public static Moment Parse(ReadOnlySpan<char> source) => DateTime.Parse(source, CultureInfo.InvariantCulture);
        public static bool TryParse(string source, out Moment result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }
        public static bool TryParse(ReadOnlySpan<char> source, out Moment result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }

        #endregion

        // Conversion

        public static implicit operator Moment(DateTime source) => new Moment(source);
        public static implicit operator Moment(DateTimeOffset source) => new Moment(source);
        public static implicit operator Moment(IntMoment source) => new Moment(source);
        public static implicit operator DateTime(Moment source) => source.ToDateTime();
        public static implicit operator DateTimeOffset(Moment source) => source.ToDateTimeOffset();
        public static implicit operator IntMoment(Moment source) => source.ToIntMoment();

        public DateTime ToDateTime() => DateTime.UnixEpoch + EpochOffset;
        public DateTimeOffset ToDateTimeOffset() => DateTimeOffset.UnixEpoch + EpochOffset;
        public IntMoment ToIntMoment() => new IntMoment(this);
        public double ToUnixEpoch() => EpochOffset.TotalSeconds;
        public long ToIntegerUnixEpoch() => (long) Math.Floor(ToUnixEpoch());

        public override string ToString() 
            => ToDateTime().ToString(CultureInfo.InvariantCulture);
        public string ToString(string format) 
            => ToDateTime().ToString(format, CultureInfo.InvariantCulture);
        public string ToString(string format, CultureInfo cultureInfo) 
            => ToDateTime().ToString(format, cultureInfo);

        // Equality
        
        public bool Equals(Moment other) => EpochOffset.Equals(other.EpochOffset);
        public int CompareTo(Moment other) => EpochOffset.CompareTo(other.EpochOffset);
        public override bool Equals(object? obj) => obj is Moment other && Equals(other);
        public override int GetHashCode() => EpochOffset.GetHashCode();
        public static bool operator ==(Moment left, Moment right) => left.Equals(right);
        public static bool operator !=(Moment left, Moment right) => !left.Equals(right);
        
        // Operations
        
        public static bool operator >(Moment t1, Moment t2) => t1.EpochOffset > t2.EpochOffset;
        public static bool operator >=(Moment t1, Moment t2) => t1.EpochOffset >= t2.EpochOffset;
        public static bool operator <(Moment t1, Moment t2) => t1.EpochOffset < t2.EpochOffset;
        public static bool operator <=(Moment t1, Moment t2) => t1.EpochOffset <= t2.EpochOffset;
        public static Moment operator +(Moment t1, TimeSpan t2) => new Moment(t1.EpochOffset + t2);
        public static Moment operator -(Moment t1, TimeSpan t2) => new Moment(t1.EpochOffset - t2);
        public static TimeSpan operator -(Moment t1, Moment t2) => t1.EpochOffset - t2.EpochOffset;
    }
}
