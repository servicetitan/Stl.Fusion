using System;

namespace Stl.Testing
{
    public static class WebHostTestEx
    {
        public static readonly Random Random = new Random();

        public static string GetRandomLocalUrl() => $"http://localhost:{GetRandomPort()}";

        public static int GetRandomPort()
        {
            lock (Random) {
                return Random.Next(10000) + 40000;
            }
        }
    }
}
