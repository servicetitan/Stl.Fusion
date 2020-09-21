using System.Threading;
using Stl.DependencyInjection;

namespace Stl.Fusion.Authentication
{
    [Service(typeof(IAuthSessionAccessor))]
    public class AsyncLocalAuthSessionAccessor : IAuthSessionAccessor
    {
        private readonly AsyncLocal<AuthSession?> _sessionAsyncLocal = new AsyncLocal<AuthSession?>();

        public AuthSession? Session {
            get => _sessionAsyncLocal.Value ?? DefaultSession;
            set => _sessionAsyncLocal.Value = value;
        }

        public AuthSession? DefaultSession { get; set; }
    }
}
