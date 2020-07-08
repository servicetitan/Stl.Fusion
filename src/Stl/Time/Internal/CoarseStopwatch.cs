using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Security.Cryptography;
using System.Threading;
using System.Threading.Tasks;

namespace Stl.Time.Internal
{
    public static class CoarseStopwatch
    {
        public static readonly int Frequency = 20;
        public static readonly Moment Start;
        public static readonly long StartEpochOffsetTicks;

        private static readonly Stopwatch Stopwatch;
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
            get => new Moment(NowEpochOffsetTicks);
        }

        public static long RandomInt64 {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => Volatile.Read(ref _randomInt64);
        }

        public static int RandomInt32 {
            [MethodImpl(MethodImplOptions.AggressiveInlining)]
            get => _randomInt32;
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
            var rnd = RandomNumberGenerator.Create();
            var buffer = new long[1];

            void Update() {
                // Updating _elapsedTicks
                Interlocked.Exchange(ref _elapsedTicks, Stopwatch.ElapsedTicks);
                
                // Updating _random*
                var bufferSpan = MemoryMarshal.Cast<long, byte>(buffer.AsSpan());
                rnd!.GetBytes(bufferSpan);
                var randomInt64 = buffer![0];
                var randomInt32 = unchecked((int) randomInt64);
                Interlocked.Exchange(ref _randomInt64, randomInt64);
                _randomInt32 = randomInt32;
            }

            try {
                // Dedicated thread is preferable here, since
                // we need to adjust its priority.
                var t = new Thread(() => {
                    var interval = TimeSpan.FromSeconds(1.0 / Frequency);
                    while (true) {
                        Update();
                        Thread.Sleep(interval);
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
                        Update();
                        await Task.Delay(interval).ConfigureAwait(false);
                    }
                    // ReSharper disable once FunctionNeverReturns
                });
            }
        }
    }
}
