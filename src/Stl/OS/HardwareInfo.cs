using System;
using System.Threading;
using Stl.Mathematics;

namespace Stl.OS
{
    public class HardwareInfo
    {
        private const int RefreshIntervalTicks = 30_000; // Tick = millisecond
        private static volatile int _lastRefreshTicks;
        private static volatile int _processorCount; 
        private static volatile int _processorCountPo2; 

        public static int ProcessorCount {
            get {
                MaybeRefresh();
                return _processorCount;
            }
        }

        public static int ProcessorCountPo2 {
            get {
                MaybeRefresh();
                return _processorCountPo2;
            }
        }

        private static void MaybeRefresh()
        {
            var now = Environment.TickCount;
            var lastRefreshTicks = _lastRefreshTicks;
            if (lastRefreshTicks != 0 && now - lastRefreshTicks < RefreshIntervalTicks)
                // No need to refresh
                return;
            if (lastRefreshTicks != Interlocked.CompareExchange(ref _lastRefreshTicks, now, lastRefreshTicks))
                // Some other thread is already updating these values
                return;
            _processorCount = Math.Max(1, Environment.ProcessorCount);
            _processorCountPo2 = Math.Max(1, (int) Bits.GreaterOrEqualPowerOf2((uint) _processorCount));
        }
    }
}
