using System;
using System.Collections.Generic;

namespace Stl.Testing 
{
    public static class Delays
    {
        public static IEnumerable<TimeSpan> Fixed(TimeSpan delay)
        {
            while (true)
                yield return delay;
        }
            
        public static IEnumerable<TimeSpan> Exponential(TimeSpan delay, double factor)
        {
            while (true) {
                yield return delay;
                delay *= factor;
            }
        }
    }
}
