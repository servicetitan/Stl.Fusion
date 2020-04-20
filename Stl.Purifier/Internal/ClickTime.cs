using System;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Stl.Time;

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

        public static readonly Moment Start;
        public static int Clicks => _clicks;
        public static Moment Now => ClicksToMoment(_clicks);

        static ClickTime()
        {
            Start = ReadTime();
            _clicks = 0;
            BeginUpdates();
        }

        // Conversion helpers

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static TimeSpan ClicksToTimeSpan(int clicks) 
            => new TimeSpan(clicks * TicksPerClick);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static Moment ClicksToMoment(int clicks) 
            => Start + new TimeSpan(clicks * TicksPerClick);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int TimeSpanToClicks(TimeSpan timeSpan) 
            => (int) (timeSpan.Ticks / TicksPerClick);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        public static int MomentToClicks(Moment moment) 
            => TimeSpanToClicks(moment - Start);

        [MethodImpl(MethodImplOptions.AggressiveInlining)]
        private static Moment ReadTime() => RealTimeClock.HighResolutionNow; 

        // Manual update; you normally shouldn't call it 
        public static void Update()
        {
            var newOffset = (int) ((ReadTime() - Start).Ticks / TicksPerClick);
            Interlocked.Exchange(ref _clicks, newOffset);
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
