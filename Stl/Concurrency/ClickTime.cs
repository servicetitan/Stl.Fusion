using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Purifier.Internal
{
    // A high performance timer. Use its Clicks property when you need 
    // extremely fast timer reads.
    public static class ClickTime
    {
        // 1/20 of a second allows to run this timer for ~ 3.4 years
        private const long ClicksPerSecond = 20;
        private const long TicksPerClick = TimeSpan.TicksPerSecond / ClicksPerSecond;
        private static volatile int _clicks;
        private static readonly Stopwatch Stopwatch;

        public static readonly DateTime Start;
        public static int Clicks => _clicks;
        public static DateTime Now => ClicksToDateTime(_clicks);

        static ClickTime()
        {
            Start = DateTime.Now;
            _clicks = 0;
            Stopwatch = Stopwatch.StartNew();
            BeginUpdates();
        }

        // Conversion helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan ClicksToTimeSpan(int clicks) 
            => new TimeSpan(clicks * TicksPerClick);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static double ClicksToSeconds(int clicks) 
            => new TimeSpan(clicks * TicksPerClick).TotalSeconds;
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static DateTime ClicksToDateTime(int clicks) 
            => Start + new TimeSpan(clicks * TicksPerClick);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TimeSpanToClicks(TimeSpan timeSpan) 
            => (int) (timeSpan.Ticks / TicksPerClick);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int SecondsToClicks(double seconds) 
            => (int) (TimeSpan.FromSeconds(seconds).Ticks / TicksPerClick);
        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int DateTimeToClicks(DateTime moment) 
            => TimeSpanToClicks(moment - Start);

        // Manual update; you normally shouldn't call it 
        public static void Update()
        {
            var clicks = (int) (Stopwatch.ElapsedTicks / TicksPerClick);
            Interlocked.Exchange(ref _clicks, clicks);
        }

        private static async void BeginUpdates()
        {
            var interval = new TimeSpan(TicksPerClick / 2);
            while (true) {
                await Task.Delay(interval).ConfigureAwait(false);
                Update();
            }
        }
    }
}
