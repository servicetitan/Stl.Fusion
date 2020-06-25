using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time.Internal
{
    internal static class CoarseStopwatch
    {
        public static readonly int Frequency = 20;
        public static readonly Moment Start;
        public static readonly long StartEpochOffsetTicks;

        private static readonly Stopwatch Stopwatch;
        private static long _elapsedTicks;

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
            get => new Moment(NowEpochOffsetTicks);
        }

        static CoarseStopwatch()
        {
            Start = SystemClock.Now;
            Stopwatch = Stopwatch.StartNew();
            StartEpochOffsetTicks = Start.EpochOffset.Ticks;
            BeginUpdates();
        }

        private static void BeginUpdates()
        {
            try {
                // Dedicated thread is preferable here, since
                // we need to adjust its priority.
                var t = new Thread(() => {
                    var interval = TimeSpan.FromSeconds(1.0 / Frequency);
                    while (true) {
                        Thread.Sleep(interval);
                        Interlocked.Exchange(ref _elapsedTicks, Stopwatch.ElapsedTicks);
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
                    var interval = TimeSpan.FromSeconds(1.0 / Frequency);
                    while (true) {
                        await Task.Delay(interval).ConfigureAwait(false);
                        Interlocked.Exchange(ref _elapsedTicks, Stopwatch.ElapsedTicks);
                    }
                    // ReSharper disable once FunctionNeverReturns
                });
            }
        }
    }
}
