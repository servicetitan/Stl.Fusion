using System;

namespace Stl.Time.Internal
{
    public static class Errors
    {
        public static Exception UnusableClock() =>
            new NotSupportedException("These clock cannot be used.");
    }
}