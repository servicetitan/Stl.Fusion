using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Time;

namespace Stl.Fusion.Extensions.Internal
{
    public class KeyValueStoreSandboxProvider : IKeyValueStoreSandboxProvider
    {
        public class Options
        {
            public bool PreferUserKeyScope { get; set; } = true;
            public string UserKeyPrefixFormat { get; set; } = "_sandbox/user/{0}";
            public TimeSpan? UserKeyExpirationTime { get; set; } = null;
            public string SessionKeyPrefixFormat { get; set; } = "_sandbox/session/{0}";
            public TimeSpan? SessionKeyExpirationTime { get; set; } = TimeSpan.FromDays(30);
            public IMomentClock? Clock { get; set; } = null;
        }

        public bool PreferUserKeyScope { get; }
        public string UserKeyPrefixFormat { get; }
        public TimeSpan? UserKeyExpirationTime { get; }
        public string SessionKeyPrefixFormat { get; }
        public TimeSpan? SessionKeyExpirationTime { get; }
        protected IMomentClock Clock { get; }
        protected IAuthService AuthService { get; }

        public KeyValueStoreSandboxProvider(Options? options, IServiceProvider services)
        {
            options ??= new Options();
            PreferUserKeyScope = options.PreferUserKeyScope;
            UserKeyPrefixFormat = options.UserKeyPrefixFormat;
            UserKeyExpirationTime = options.UserKeyExpirationTime;
            SessionKeyPrefixFormat = options.SessionKeyPrefixFormat;
            SessionKeyExpirationTime = options.SessionKeyExpirationTime;
            Clock = options.Clock ?? services.GetService<IMomentClock>() ?? SystemClock.Instance;
            AuthService = services.GetRequiredService<IAuthService>();
        }

        public async Task<KeyValueStoreSandbox> GetSandbox(Session session, CancellationToken cancellationToken = default)
        {
            // This method is called from controller with [Publish],
            // which captures the first IComputed it "sees" - and it
            // is going to be AuthService.GetUser computed unless we
            // suppress ComputeContext here.
            using var _ = ComputeContext.Suppress();
            var user = await AuthService.GetUser(session, cancellationToken);
            return user.IsAuthenticated
                ? new KeyValueStoreSandbox(string.Format(UserKeyPrefixFormat, user.Id), Clock.Now + UserKeyExpirationTime)
                : new KeyValueStoreSandbox(string.Format(SessionKeyPrefixFormat, session.Id), Clock.Now + SessionKeyExpirationTime);
        }
    }
}
