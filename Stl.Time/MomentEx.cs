using System;

namespace Stl.Time
{
    public static class MomentEx
    {
        public static Moment ToMoment(this DateTime source) => new Moment(source);
        public static Moment ToMoment(this DateTimeOffset source) => new Moment(source);
    }
}