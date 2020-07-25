using System;
using System.Collections.Generic;

namespace Stl.Collections
{
    public static class Intervals
    {
        public static IEnumerable<TimeSpan> Fixed(TimeSpan delay)
        {
            while (true)
                yield return delay;
        }

        public static IEnumerable<TimeSpan> Exponential(TimeSpan delay, double factor, TimeSpan? maxDelay = null)
        {
            while (true) {
                if (maxDelay.HasValue && delay > maxDelay.GetValueOrDefault())
                    delay = maxDelay.GetValueOrDefault();
                yield return delay;
                delay *= factor;
            }
        }
    }
}
