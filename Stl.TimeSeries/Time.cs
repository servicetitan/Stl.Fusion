using System;
using System.Globalization;

namespace Stl.TimeSeries
{
    // Custom wrapper type to represent time is used here to be able to change its structure later. 
    [Serializable]
    public readonly struct Time : IEquatable<Time>, IComparable<Time>
    {
        public static Time Zero = new Time();
        public static Time Now => new Time(DateTime.Now);

        public TimeSpan UnixTime { get; }

        public Time(TimeSpan value) => UnixTime = value;
        public Time(DateTime value) => UnixTime = value - DateTime.UnixEpoch;
        public Time(DateTimeOffset value) => UnixTime = value - DateTimeOffset.UnixEpoch;

        #region Parse functions
        
        public static Time Parse(string source) => DateTime.Parse(source, CultureInfo.InvariantCulture);
        public static Time Parse(ReadOnlySpan<char> source) => DateTime.Parse(source, CultureInfo.InvariantCulture);
        public static bool TryParse(string source, out Time result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }
        public static bool TryParse(ReadOnlySpan<char> source, out Time result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }

        #endregion

        public DateTime ToDateTime() => DateTime.UnixEpoch + UnixTime;
        public DateTimeOffset ToDateTimeOffset() => DateTimeOffset.UnixEpoch + UnixTime;
        public override string ToString() => ToDateTime().ToString(CultureInfo.InvariantCulture);

        public bool Equals(Time other) => UnixTime.Equals(other.UnixTime);
        public int CompareTo(Time other) => UnixTime.CompareTo(other.UnixTime);
        public override bool Equals(object? obj) => obj is Time other && Equals(other);
        public override int GetHashCode() => UnixTime.GetHashCode();

        public static bool operator ==(Time left, Time right) => left.Equals(right);
        public static bool operator !=(Time left, Time right) => !left.Equals(right);
        public static bool operator >(Time t1, Time t2) => t1 > t2;
        public static bool operator >=(Time t1, Time t2) => t1 >= t2;
        public static bool operator <(Time t1, Time t2) => t1 < t2;
        public static bool operator <=(Time t1, Time t2) => t1 <= t2;
        public static Time operator +(Time d1, TimeSpan d2) => new Time(d1.UnixTime + d2);
        public static Time operator -(Time d1, TimeSpan d2) => new Time(d1.UnixTime - d2);
        public static TimeSpan operator -(Time d1, Time d2) => d1.UnixTime - d2.UnixTime;

        public static implicit operator Time(DateTime source) => new Time(source);
        public static implicit operator Time(DateTimeOffset source) => new Time(source);
        public static explicit operator DateTime(Time source) => source.ToDateTime();
        public static explicit operator DateTimeOffset(Time source) => source.ToDateTimeOffset();
    }
}
