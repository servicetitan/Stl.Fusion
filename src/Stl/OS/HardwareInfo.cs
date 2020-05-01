using System;
using Stl.Mathematics;

namespace Stl.OS
{
    public class HardwareInfo
    {
        private const int RefreshIntervalTicks = 30000; // Tick = millisecond
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
            if (_lastRefreshTicks != 0 && now - _lastRefreshTicks < RefreshIntervalTicks)
                return;
            _lastRefreshTicks = now;
            _processorCount = Environment.ProcessorCount;
            _processorCountPo2 = (int) Bits.GreaterOrEqualPowerOf2((uint) _processorCount);

        }
    }
}
