using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.Async;

namespace Stl.Fusion.Authentication
{
    public class PresenceService : AsyncProcessBase
    {
        public class Options
        {
            public TimeSpan UpdatePeriod { get; set; } = TimeSpan.FromMinutes(3);
        }

        protected ILogger Log { get; }
        protected IAuthService AuthService { get; }
        protected ISessionResolver SessionResolver { get; }
        protected IUpdateDelayer UpdateDelayer { get; }

        public PresenceService(
            Options? options,
            IAuthService authService,
            ISessionResolver sessionResolver,
            ILogger<PresenceService>? log = null)
        {
            options ??= new();
            Log = log ?? NullLogger<PresenceService>.Instance;
            AuthService = authService;
            SessionResolver = sessionResolver;
            UpdateDelayer = new UpdateDelayer(new UpdateDelayer.Options() {
                DelayDuration = options.UpdatePeriod,
                CancellationDelay = TimeSpan.Zero,
            });
        }

        public virtual void UpdatePresence() => UpdateDelayer.CancelDelays();

        protected override async Task RunInternal(CancellationToken cancellationToken)
        {
            var session = await SessionResolver.GetSession(cancellationToken).ConfigureAwait(false);
            var retryCount = 0;
            while (!cancellationToken.IsCancellationRequested) {
                await UpdateDelayer.Delay(retryCount, cancellationToken).ConfigureAwait(false);
                var success = await UpdatePresence(session, cancellationToken).ConfigureAwait(false);
                retryCount = success ? 0 : 1 + retryCount;
            }
        }

        protected virtual async Task<bool> UpdatePresence(Session session, CancellationToken cancellationToken)
        {
            try {
                await AuthService.UpdatePresence(session, cancellationToken).ConfigureAwait(false);
                return true;
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch (Exception e) {
                Log.LogError(e, "UpdatePresenceAsync error");
                return false;
            }
        }
    }
}
