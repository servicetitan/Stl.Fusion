using System;

namespace Stl.OS
{
    public class HardwareInfo
    {
        private const int RefreshIntervalTicks = 30000; // Tick = millisecond
        private static volatile int _lastRefreshTicks;
        private static volatile int _processorCount; 

        public static int ProcessorCount {
            get {
                MaybeRefresh();
                return _processorCount;
            }
        }

        private static void MaybeRefresh()
        {
            var now = Environment.TickCount;
            if (_lastRefreshTicks != 0 && now - _lastRefreshTicks < RefreshIntervalTicks)
                return;
            _lastRefreshTicks = now;
            _processorCount = Environment.ProcessorCount;
        }
    }
}
