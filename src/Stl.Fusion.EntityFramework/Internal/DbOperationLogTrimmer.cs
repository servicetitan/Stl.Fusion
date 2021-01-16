using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Stl.Generators;

namespace Stl.Fusion.EntityFramework.Internal
{
    public class DbOperationLogTrimmer<TDbContext> : DbWakeSleepProcessBase<TDbContext>
        where TDbContext : DbContext
    {
        public class Options
        {
            public TimeSpan CheckInterval { get; set; } = TimeSpan.FromMinutes(10);
            public TimeSpan MaxOperationAge { get; set; } = TimeSpan.FromHours(1);
        }

        protected IDbOperationLog<TDbContext> DbOperationLog { get; }
        protected TimeSpan CheckInterval { get; }
        protected TimeSpan MaxCommitAge { get; }
        protected Random Random { get; }

        public DbOperationLogTrimmer(Options? options, IServiceProvider services)
            : base(services)
        {
            options ??= new();
            CheckInterval = options.CheckInterval;
            MaxCommitAge = options.MaxOperationAge;
            DbOperationLog = services.GetRequiredService<IDbOperationLog<TDbContext>>();
            Random = new Random();
        }

        protected override async Task WakeAsync(CancellationToken cancellationToken)
        {
            var minCommitTime = (Clock.Now - MaxCommitAge).ToDateTime();
            await DbOperationLog.TrimAsync(minCommitTime, cancellationToken).ConfigureAwait(false);
        }

        protected override Task SleepAsync(Exception? error, CancellationToken cancellationToken)
        {
            var delay = CheckInterval + TimeSpan.FromMilliseconds(100 * Random.NextDouble());
            return Clock.DelayAsync(delay, cancellationToken);
        }
    }
}
