using Microsoft.Extensions.DependencyInjection;
using Newtonsoft.Json;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.OS;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(PerformanceTests)), Trait("Category", nameof(PerformanceTests))]
public abstract class PerformanceTestBase : FusionTestBase
{
    public int UserCount = 1000;

    protected PerformanceTestBase(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options)
    { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);
        var users = Services.GetRequiredService<IUserService>();
        var tasks = new List<Task>();
        for (var i = 0; i < UserCount; i++)
            tasks.Add(users.Create(new IUserService.AddCommand(new User() {
                Id = i,
                Name = $"User_{i}",
            }, true)));
        await Task.WhenAll(tasks);
    }

    // [Fact]
    [Fact(Skip = "Performance")]
    public async Task ComputedPerformanceTest()
    {
        if (TestRunnerInfo.IsBuildAgent())
            return; // Shouldn't run this test on build agents

        var users = Services.GetRequiredService<IUserService>();
        var plainUsers = Services.GetRequiredService<UserService>();
        var useImdb = Options.DbType == FusionTestDbType.InMemory;
        var opCountPerCore = 2_000_000;
        var readersPerCore = 4;
        var readerCount = HardwareInfo.GetProcessorCountFactor(readersPerCore);
        var cachingIterationCount = opCountPerCore / readersPerCore;
        var nonCachingIterationCount = cachingIterationCount / (useImdb ? 1000 : 10_000);

        var withoutSerialization = (Action<User>) (u => { });
        var withSerialization = (Action<User>) (u => JsonConvert.SerializeObject(u));

        Out.WriteLine($".NET: {RuntimeInfo.DotNetCore.VersionString}");
        Out.WriteLine($"Database: {(useImdb ? "In-memory" : "Sqlite")}");
        Out.WriteLine("With Stl.Fusion:");
        await Test("Standard test", users, withoutSerialization,
            readerCount, cachingIterationCount);
        await Test("Standard test + serialization", users, withSerialization,
            readerCount, cachingIterationCount / 3);

        Out.WriteLine("Without Stl.Fusion:");
        await Test("Standard test", plainUsers, withoutSerialization,
            readerCount, nonCachingIterationCount);
        await Test("Standard test + serialization", plainUsers, withSerialization,
            readerCount, nonCachingIterationCount);
    }

    private async Task Test(string title,
        IUserService users, Action<User> extraAction,
        int threadCount, int iterationCount, bool isWarmup = false)
    {
        if (!isWarmup)
            await Test(title, users, extraAction, threadCount, iterationCount / 10, true);

        async Task Mutator(string name, CancellationToken cancellationToken)
        {
            var rnd = new Random();
            var count = 0L;
            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var userId = (long) rnd.Next(UserCount);
                // Log.LogDebug($"{name}: R {userId}");
                var user = await users.Get(userId, cancellationToken)
                    .ConfigureAwait(false);
                user = user! with { Email = $"{++count}@counter.org" };
                // Log.LogDebug($"{name}: R done, U {user}");
                await users.Update(new(user), cancellationToken).ConfigureAwait(false);
                // Log.LogDebug($"{name}: U {user} done");
                await Task.Delay(10, cancellationToken).ConfigureAwait(false);
            }
        }

        async Task<long> Reader(string name, int iterationCount)
        {
            var rnd = new Random();
            var count = 0L;
            for (; iterationCount > 0; iterationCount--) {
                var userId = (long) rnd.Next(UserCount);
                // Log.LogDebug($"{name}: R {userId}");
                var user = await users.Get(userId).ConfigureAwait(false);
                // Log.LogDebug($"{name}: R {userId} done");
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

        WriteLine($"  {title}:");
        WriteLine($"    Operations: {operationCount} ({threadCount} readers x {iterationCount}");

        var startTime = CpuClock.Now;
        var mutatorTask = Task.Run(() => Mutator("W", stopCts.Token));
        var tasks = Enumerable
            .Range(0, threadCount)
            .Select(i => Task.Run(() => Reader($"R{i}", iterationCount)))
            .ToArray();
        var results = await Task.WhenAll(tasks);
        var elapsed = CpuClock.Now - startTime;

        stopCts.Cancel();
        await mutatorTask.SuppressExceptions().ConfigureAwait(false);

        WriteLine($"    Duration:   {elapsed.TotalSeconds:F3} sec");
        WriteLine($"    Speed:      {operationCount / 1000.0 / elapsed.TotalSeconds:F3} K Ops/sec");

        results.Length.Should().Be(threadCount);
        results.All(r => r == iterationCount).Should().BeTrue();
    }
}

public class PerformanceTest_Sqlite : PerformanceTestBase
{
    public PerformanceTest_Sqlite(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            UseLogging = false,
        })
    { }
}

public class PerformanceTest_Npgsql : PerformanceTestBase
{
    public PerformanceTest_Npgsql(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            UseLogging = false,
            DbType = FusionTestDbType.PostgreSql,
        })
    { }
}

public class PerformanceTest_InMemoryDb : PerformanceTestBase
{
    public PerformanceTest_InMemoryDb(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.InMemory,
            UseLogging = false,
        })
    { }
}
