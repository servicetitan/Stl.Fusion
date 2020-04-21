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
        public TimeSpan UnixTime { get; }
        public static readonly Moment MaxValue = new Moment(TimeSpan.MaxValue); 
        public static readonly Moment MinValue = new Moment(TimeSpan.MinValue); 
        public static readonly Moment Zero = new Moment(TimeSpan.Zero); 

        public Moment(TimeSpan value) => UnixTime = value;
        public Moment(DateTime value) => UnixTime = value.ToUniversalTime() - DateTime.UnixEpoch;
        public Moment(DateTimeOffset value) => UnixTime = value.ToUniversalTime() - DateTimeOffset.UnixEpoch;

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
        public static implicit operator DateTime(Moment source) => source.ToDateTime();
        public static implicit operator DateTimeOffset(Moment source) => source.ToDateTimeOffset();

        public DateTime ToDateTime() => DateTime.UnixEpoch + UnixTime;
        public DateTimeOffset ToDateTimeOffset() => DateTimeOffset.UnixEpoch + UnixTime;
        public double ToUnixEpoch() => UnixTime.TotalSeconds;
        public long ToIntegerUnixEpoch() => (long) Math.Floor(UnixTime.TotalSeconds);

        public override string ToString() 
            => ToDateTime().ToString(CultureInfo.InvariantCulture);
        public string ToString(string format) 
            => ToDateTime().ToString(format, CultureInfo.InvariantCulture);
        public string ToString(string format, CultureInfo cultureInfo) 
            => ToDateTime().ToString(format, cultureInfo);

        // Equality
        
        public bool Equals(Moment other) => UnixTime.Equals(other.UnixTime);
        public int CompareTo(Moment other) => UnixTime.CompareTo(other.UnixTime);
        public override bool Equals(object? obj) => obj is Moment other && Equals(other);
        public override int GetHashCode() => UnixTime.GetHashCode();
        public static bool operator ==(Moment left, Moment right) => left.Equals(right);
        public static bool operator !=(Moment left, Moment right) => !left.Equals(right);
        
        // Operations
        
        public static bool operator >(Moment t1, Moment t2) => t1.UnixTime > t2.UnixTime;
        public static bool operator >=(Moment t1, Moment t2) => t1.UnixTime >= t2.UnixTime;
        public static bool operator <(Moment t1, Moment t2) => t1.UnixTime < t2.UnixTime;
        public static bool operator <=(Moment t1, Moment t2) => t1.UnixTime <= t2.UnixTime;
        public static Moment operator +(Moment t1, TimeSpan t2) => new Moment(t1.UnixTime + t2);
        public static Moment operator -(Moment t1, TimeSpan t2) => new Moment(t1.UnixTime - t2);
        public static TimeSpan operator -(Moment t1, Moment t2) => t1.UnixTime - t2.UnixTime;
    }
}
