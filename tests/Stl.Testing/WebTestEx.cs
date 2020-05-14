using System;
using System.Threading;

namespace Stl.Testing
{
    public static class WebTestEx
    {
        private static volatile int _lastUsedPortOffset = 0;

        public static Uri ToWss(this Uri uri)
        {
            var url = uri.ToString();
            if (url.StartsWith("http://"))
                return new Uri("ws://" + url.Substring(7)); 
            if (url.StartsWith("https://"))
                return new Uri("wss://" + url.Substring(8)); 
            return uri;
        }

        public static Uri GetLocalUri(int port) => new Uri($"http://localhost:{port}");

        public static Uri GetRandomLocalUri() => GetLocalUri(GetRandomPort());
        public static int GetRandomPort()
        {
            var portOffset = Interlocked.Increment(ref _lastUsedPortOffset);
            return 25000 + (portOffset % 25000);
        }
    }
}
