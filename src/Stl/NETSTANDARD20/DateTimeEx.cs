#if NETSTANDARD2_0

namespace System
{
    public static class DateTimeEx
    {
        public static readonly DateTime UnixEpoch = new DateTime(621355968000000000L, DateTimeKind.Utc);
    }
    
    public static class DateTimeOffsetEx
    {
        public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(621355968000000000L, TimeSpan.Zero);
    }
}

#endif