using System;

namespace Stl.Samples.Blazor.Client.Shared
{
    public static class FormatEx
    {
        public static string Format(this DateTime dateTime) 
            => dateTime.ToString("HH:mm:ss.ffff");
        
        public static string Format(this DateTime? dateTime) 
            => dateTime?.ToString("HH:mm:ss.ffff") ?? "n/a";
    }
}
