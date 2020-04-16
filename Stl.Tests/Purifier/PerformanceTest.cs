using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stl.Async;
using Stl.Concurrency;
using Stl.OS;
using Stl.Pooling;
using Stl.Tests.Purifier.Model;
using Stl.Tests.Purifier.Services;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public abstract class PerformanceTestBase : PurifierTestBase, IAsyncLifetime
    {
        public int UserCount = 100;

        protected PerformanceTestBase(ITestOutputHelper @out, PurifierTestOptions? options = null) 
            : base(@out, options)
            => IsLoggingEnabled = false;

        public override async Task InitializeAsync()
        {
            await base.InitializeAsync();

            var users = Container.Resolve<IUserProvider>();
            var tasks = new List<Task>();
            for (var i = 0; i < UserCount; i++)
                tasks.Add(users.CreateAsync(new User() {
                    Id = i,
                    Name = $"User_{i}",
                }, true));
            await Task.WhenAll(tasks);
        }

        [Fact]
        public async Task ComputedPerformanceTest()
        {
            var users = Services.GetRequiredService<IUserProvider>();
            var threadCount = HardwareInfo.ProcessorCount * 10;
            var iterationCount = Options.UseInMemoryDatabase ? 50_000 : 10_000;

            var cachingProviderPool = new ConcurrentPool<IUserProvider>(() => users);
            var nonCachingProviderPool = new ConcurrentPool<IUserProvider>(() => {
                var scope = Container.BeginLifetimeScope();
                return scope.Resolve<UserProvider>();
                // No scope disposal, but it's fine for the test, I guess
            });

            var extraAction = (Action<User>) (user => { });
            await Test("Caching providers", cachingProviderPool, extraAction, 
                threadCount, iterationCount);
            await Test("Non-caching providers", nonCachingProviderPool, extraAction, 
                threadCount, iterationCount / 300);

            extraAction = user => JsonConvert.SerializeObject(user);
            await Test("Caching providers + serialization", cachingProviderPool, extraAction, 
                threadCount, iterationCount / 10);
            await Test("Non-caching providers + serialization", nonCachingProviderPool, extraAction, 
                threadCount, iterationCount / 300);
        }

        private async Task Test(string title, 
            IPool<IUserProvider> userProviderPool, Action<User> extraAction, 
            int threadCount, int iterationCount, bool isWarmup = false)
        {
            if (!isWarmup)
                // Warmup
                await Test(title, userProviderPool, extraAction, threadCount, iterationCount / 100, true);

            async Task Mutator(CancellationToken cancellationToken)
            {
                // return;
                using var lease = userProviderPool.Rent();
                var users = lease.Resource;
                var rnd = new Random();
                var count = 0L;
                while (true) {
                    cancellationToken.ThrowIfCancellationRequested();
                    var userId = (long) rnd.Next(UserCount);
                    var user = await users.TryGetAsync(userId, cancellationToken)
                        .ConfigureAwait(false);
                    user = user!.ToUnfrozen();
                    user!.Email = $"{++count}@counter.org";
                    await users.UpdateAsync(user, cancellationToken).ConfigureAwait(false);
                    await Task.Delay(10, cancellationToken).ConfigureAwait(false);
                }
            }

            async Task<long> Reader(int iterationCount)
            {
                using var lease = userProviderPool.Rent();
                var users = lease.Resource;
                var rnd = new Random();
                var count = 0L;
                for (; iterationCount > 0; iterationCount--) {
                    var userId = (long) rnd.Next(UserCount);
                    var user = await users.TryGetAsync(userId).ConfigureAwait(false);
                    if (user!.Id == userId)
                        count++;
                    extraAction.Invoke(user!);
                }
                return count;
            }

            void WriteLine(string line)
            {
                if (!isWarmup)
                    Out.WriteLine(line);
            }

            var operationCount = threadCount * iterationCount;
            var stopCts = new CancellationTokenSource();

            WriteLine($"{title}:");
            WriteLine($"  Threads:    {threadCount}");
            WriteLine($"  Iterations: {iterationCount}");

            var startTime = RealTimeClock.HighResolutionNow;
            var mutatorTask = Task.Run(() => Mutator(stopCts.Token)); 
            var tasks = Enumerable
                .Range(0, threadCount)
                .Select(_ => Task.Run(() => Reader(iterationCount)))
                .ToArray();
            var results = await Task.WhenAll(tasks);
            var elapsed = RealTimeClock.HighResolutionNow - startTime;

            stopCts.Cancel();
            await mutatorTask.SuppressExceptions().ConfigureAwait(false);

            WriteLine($"  Duration:   {elapsed.TotalSeconds:F3} sec");
            WriteLine($"  Speed:      {operationCount / 1000.0 / elapsed.TotalSeconds:F3} K Ops/sec");

            results.Length.Should().Be(threadCount);
            results.All(r => r == iterationCount).Should().BeTrue();
        }
    }

    public class PerformanceTest_Sqlite : PerformanceTestBase
    {
        public PerformanceTest_Sqlite(ITestOutputHelper @out) 
            : base(@out, new PurifierTestOptions() { UseInMemoryDatabase = false })
        { }
    }

    public class PerformanceTest_InMemoryDb : PerformanceTestBase
    {
        public PerformanceTest_InMemoryDb(ITestOutputHelper @out) 
            : base(@out, new PurifierTestOptions() { UseInMemoryDatabase = true })
        { }
    }
}
