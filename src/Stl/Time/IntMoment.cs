using System;
using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Newtonsoft.Json;
using Stl.OS;
using Stl.Time.Internal;

namespace Stl.Time
{
    [Serializable]
    [JsonConverter(typeof(IntMomentJsonConverter))]
    [TypeConverter(typeof(IntMomentTypeConverter))]
    public readonly struct IntMoment : IEquatable<IntMoment>, IComparable<IntMoment>
    {
        public const int UnitsPerSecond = 20;
        public const long TicksPerUnit = TimeSpan.TicksPerSecond / UnitsPerSecond;

        public static class Clock
        {
            public static readonly Moment EpochStartMoment;
            public static readonly DateTime EpochStartDateTimeUtc;
            public static readonly DateTimeOffset EpochStartDateTimeOffsetUtc;
            private static readonly Stopwatch Stopwatch;
            private static volatile int _epochOffsetUnits;

            public static int EpochOffsetUnits => _epochOffsetUnits;

            static Clock()
            {
                EpochStartDateTimeUtc = DateTime.UtcNow;
                EpochStartDateTimeOffsetUtc = EpochStartDateTimeUtc;
                EpochStartMoment = EpochStartDateTimeUtc;
                _epochOffsetUnits = 0;
                Stopwatch = Stopwatch.StartNew();
                BeginUpdates();
            }

            public static void Update()
            {
                var clicks = (int) (Stopwatch.ElapsedTicks / TicksPerUnit);
                Interlocked.Exchange(ref _epochOffsetUnits, clicks);
            }

            private static void BeginUpdates()
            {
                try {
                    // Dedicated thread is preferable here, since
                    // we need to adjust its priority.
                    var t = new Thread(() => {
                        var interval = new TimeSpan(TicksPerUnit / 2);
                        while (true) {
                            Thread.Sleep(interval);
                            Update();
                        }
                        // ReSharper disable once FunctionNeverReturns
                    }, 64_000) {
                        Priority = ThreadPriority.Highest, 
                        IsBackground = true
                    };
                    t.Start();
                }
                catch (NotSupportedException) {
                    // Likely, Blazor/WASM
                    Task.Run(async () => {
                        var interval = new TimeSpan(TicksPerUnit / 2);
                        while (true) {
                            await Task.Delay(interval).ConfigureAwait(false);
                            Update();
                        }
                        // ReSharper disable once FunctionNeverReturns
                    });
                }
            }
        }

        public static readonly IntMoment MaxValue = new IntMoment(int.MaxValue); 
        public static readonly IntMoment MinValue = new IntMoment(int.MinValue); 
        public static readonly IntMoment Zero = new IntMoment(0);
        public static IntMoment Now => new IntMoment(Clock.EpochOffsetUnits);
        
        public int EpochOffsetUnits { get; }
        public TimeSpan EpochOffset => UnitsToTimeSpan(EpochOffsetUnits);

        public IntMoment(int epochOffsetUnits) 
            => EpochOffsetUnits = epochOffsetUnits;
        public IntMoment(TimeSpan epochOffset) 
            => EpochOffsetUnits = TimeSpanToUnits(epochOffset);
        public IntMoment(DateTime value) 
            : this(value.ToUniversalTime() - Clock.EpochStartDateTimeUtc) { }
        public IntMoment(DateTimeOffset value) 
            : this(value.ToUniversalTime() - Clock.EpochStartDateTimeOffsetUtc) { }
        public IntMoment(Moment value) 
            : this(value.EpochOffset - Clock.EpochStartMoment.EpochOffset) { }

        #region Parse functions
        
        public static IntMoment Parse(string source) 
            => DateTime.Parse(source, CultureInfo.InvariantCulture);
        public static IntMoment Parse(ReadOnlySpan<char> source) 
            => DateTime.Parse(source, CultureInfo.InvariantCulture);
        public static bool TryParse(string source, out IntMoment result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }
        public static bool TryParse(ReadOnlySpan<char> source, out IntMoment result)
        {
            var success = DateTime.TryParse(source, CultureInfo.InvariantCulture, DateTimeStyles.None, out var r);
            result = r;
            return success;
        }

        #endregion

        // Conversion

        public static implicit operator IntMoment(DateTime source) => new IntMoment(source);
        public static implicit operator IntMoment(DateTimeOffset source) => new IntMoment(source);
        public static implicit operator IntMoment(Moment source) => new IntMoment(source);
        public static implicit operator DateTime(IntMoment source) => source.ToDateTime();
        public static implicit operator DateTimeOffset(IntMoment source) => source.ToDateTimeOffset();
        public static implicit operator Moment(IntMoment source) => source.ToMoment();

        public DateTime ToDateTime() => Clock.EpochStartDateTimeUtc + EpochOffset;
        public DateTimeOffset ToDateTimeOffset() => Clock.EpochStartDateTimeOffsetUtc + EpochOffset;
        public DateTimeOffset ToMoment() => new Moment(this);
        public double ToUnixEpoch() => (EpochOffset + Clock.EpochStartMoment.EpochOffset).TotalSeconds;
        public long ToIntegerUnixEpoch() => (long) Math.Floor(ToUnixEpoch());

        public override string ToString() 
            => ToDateTime().ToString(CultureInfo.InvariantCulture);
        public string ToString(string format) 
            => ToDateTime().ToString(format, CultureInfo.InvariantCulture);
        public string ToString(string format, CultureInfo cultureInfo) 
            => ToDateTime().ToString(format, cultureInfo);

        // Equality
        
        public bool Equals(IntMoment other) => EpochOffsetUnits.Equals(other.EpochOffsetUnits);
        public int CompareTo(IntMoment other) => EpochOffsetUnits.CompareTo(other.EpochOffsetUnits);
        public override bool Equals(object? obj) => obj is IntMoment other && Equals(other);
        public override int GetHashCode() => EpochOffsetUnits.GetHashCode();
        public static bool operator ==(IntMoment left, IntMoment right) => left.Equals(right);
        public static bool operator !=(IntMoment left, IntMoment right) => !left.Equals(right);
        
        // Operations
        
        public static bool operator >(IntMoment t1, IntMoment t2) => t1.EpochOffsetUnits > t2.EpochOffsetUnits;
        public static bool operator >=(IntMoment t1, IntMoment t2) => t1.EpochOffsetUnits >= t2.EpochOffsetUnits;
        public static bool operator <(IntMoment t1, IntMoment t2) => t1.EpochOffsetUnits < t2.EpochOffsetUnits;
        public static bool operator <=(IntMoment t1, IntMoment t2) => t1.EpochOffsetUnits <= t2.EpochOffsetUnits;
        public static IntMoment operator +(IntMoment t1, int t2) => new IntMoment(t1.EpochOffsetUnits + t2);
        public static IntMoment operator +(IntMoment t1, TimeSpan t2) => new IntMoment(t1.EpochOffset + t2);
        public static IntMoment operator -(IntMoment t1, int t2) => new IntMoment(t1.EpochOffsetUnits - t2);
        public static IntMoment operator -(IntMoment t1, TimeSpan t2) => new IntMoment(t1.EpochOffset - t2);
        public static int operator -(IntMoment t1, IntMoment t2) => t1.EpochOffsetUnits - t2.EpochOffsetUnits;

        // Conversion methods

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan UnitsToTimeSpan(int clicks) 
            => new TimeSpan(clicks * TicksPerUnit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double UnitsToSeconds(int clicks) 
            => new TimeSpan(clicks * TicksPerUnit).TotalSeconds;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime UnitsToDateTime(int clicks) 
            => Clock.EpochStartDateTimeUtc + new TimeSpan(clicks * TicksPerUnit);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TimeSpanToUnits(TimeSpan timeSpan) 
            => (int) (timeSpan.Ticks / TicksPerUnit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToUnits(double seconds) 
            => (int) (TimeSpan.FromSeconds(seconds).Ticks / TicksPerUnit);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DateTimeToUnits(DateTime moment) 
            => TimeSpanToUnits(moment - Clock.EpochStartDateTimeUtc);
    }
}
