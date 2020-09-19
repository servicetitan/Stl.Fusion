using System.Threading;
using Stl.DependencyInjection;

namespace Stl.Fusion.Authentication
{
    [Service(typeof(ISessionAccessor))]
    public class AsyncLocalSessionAccessor : ISessionAccessor
    {
        private readonly AsyncLocal<Session?> _sessionIdAsyncLocal = new AsyncLocal<Session?>();

        public Session? Session {
            get => _sessionIdAsyncLocal.Value ?? DefaultSession;
            set => _sessionIdAsyncLocal.Value = value;
        }

        public Session? DefaultSession { get; set; }
    }
}
