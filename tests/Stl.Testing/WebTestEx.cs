using System.Threading;

namespace Stl.Testing
{
    public static class WebTestEx
    {
        private static volatile int _lastUsedPortOffset = 0;

        public static string GetRandomLocalUrl() => $"http://localhost:{GetRandomPort()}";

        public static int GetRandomPort()
        {
            var portOffset = Interlocked.Increment(ref _lastUsedPortOffset);
            return 25000 + (portOffset % 25000);
        }
    }
}
