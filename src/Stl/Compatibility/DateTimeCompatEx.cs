#if NETSTANDARD2_0

namespace System
{
    internal static class DateTimeCompatEx
    {
        public static readonly DateTime UnixEpoch = new DateTime(621355968000000000L, DateTimeKind.Utc);
    }
    
    internal static class DateTimeOffsetCompatEx
    {
        public static readonly DateTimeOffset UnixEpoch = new DateTimeOffset(621355968000000000L, TimeSpan.Zero);
    }
}

#endif