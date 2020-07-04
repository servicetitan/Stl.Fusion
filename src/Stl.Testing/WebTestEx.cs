using System;
using System.Collections.Generic;
using System.Threading;

namespace Stl.Testing
{
    public static class WebTestEx
    {
        private static readonly Queue<int> RecentlyUsedPortQueue = new Queue<int>(); 
        private static readonly HashSet<int> RecentlyUsedPorts = new HashSet<int>();
        private static readonly Random Random = new Random();

        public static Uri ToWss(this Uri uri)
        {
            var url = uri.ToString();
            if (url.StartsWith("http://"))
                return new Uri("ws://" + url.Substring(7)); 
            if (url.StartsWith("https://"))
                return new Uri("wss://" + url.Substring(8)); 
            return uri;
        }

        public static Uri GetLocalUri(int port) 
            => new Uri($"http://127.0.0.1:{port}");

        public static Uri GetRandomLocalUri() 
            => GetLocalUri(GetRandomPort());

        public static int GetRandomPort()
        {
            lock (RecentlyUsedPorts) {
                while (true) {
                    var port = 25000 + Random.Next(25000);
                    if (RecentlyUsedPorts.Add(port)) {
                        RecentlyUsedPortQueue.Enqueue(port);
                        while (RecentlyUsedPortQueue.Count > 1000) {
                            var oldPort = RecentlyUsedPortQueue.Dequeue();
                            RecentlyUsedPorts.Remove(oldPort);
                        }
                        return port;
                    }
                }
            }
        }
    }
}
