using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using Stl.Generators;

namespace Stl.Time.Internal
{
    public static class CoarseClockHelper
    {
        public static readonly int Frequency = 20;
        public static readonly Moment Start;
        public static readonly long StartEpochOffsetTicks;

        private static readonly Timer Timer;
        private static readonly Stopwatch Stopwatch;
        private static readonly RandomInt64Generator Rng = new();
        private static Moment _systemNow;
        private static long _elapsedTicks;
        private static long _randomInt64;
        private static volatile int _randomInt32;

        public static long ElapsedTicks {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _elapsedTicks);
        }

        public static long NowEpochOffsetTicks {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => StartEpochOffsetTicks + ElapsedTicks;
        }

        public static Moment Now {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => new(NowEpochOffsetTicks);
        }

        public static Moment SystemNow {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _systemNow;
        }

        public static long RandomInt64 {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _randomInt64);
        }

        public static int RandomInt32 {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _randomInt32;
        }

        static CoarseClockHelper()
        {
            var start = _systemNow = SystemClock.Now;
            Start = start;
            StartEpochOffsetTicks = start.EpochOffset.Ticks;
            Stopwatch = Stopwatch.StartNew();
            Update();
            var interval = TimeSpan.FromSeconds(1.0 / Frequency);
            Timer = NonCapturingTimer.Create(_ => Update(), null!, interval, interval);
        }

        [DebuggerStepThrough]
        private static void Update()
        {
            // Update _elapsedTicks
            Interlocked.Exchange(ref _elapsedTicks, Stopwatch.Elapsed.Ticks);

            // Update _random*
            var rnd = Rng.Next();
            Interlocked.Exchange(ref _randomInt64, rnd);
            _randomInt32 = unchecked((int) rnd);

            // Update _systemNow
            _systemNow = SystemClock.Now;
        }
    }
}
