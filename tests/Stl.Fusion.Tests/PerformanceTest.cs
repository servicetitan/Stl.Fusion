using System.Text.Json;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.OS;

namespace Stl.Fusion.Tests;

public abstract class PerformanceTestBase : FusionTestBase
{
    public int UserCount = 1000;

    protected PerformanceTestBase(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options)
    { }

    public override async Task InitializeAsync()
    {
        await base.InitializeAsync().ConfigureAwait(false);
        var commander = Services.Commander();
        var tasks = new List<Task>();
        for (var i = 0; i < UserCount; i++)
            tasks.Add(commander.Call(new IUserService.AddCommand(new User() {
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
        var opCountPerCore = 4_000_000;
        var readersPerCore = 8;
        var readerCount = HardwareInfo.GetProcessorCountFactor(readersPerCore);
        var fusionIterationCount = opCountPerCore / readersPerCore;
        var nonFusionIterationCount = fusionIterationCount / 4000;

        var withoutSerialization = (Action<User>) (_ => { });
        var withSerialization = (Action<User>) (u => JsonSerializer.Serialize(u)); // STJ serializer
        var enableSerialization = false;

        Out.WriteLine($".NET: {RuntimeInfo.DotNetCore.VersionString}");
        Out.WriteLine($"Database: {Options.DbType}");
        Out.WriteLine("With Stl.Fusion:");
        if (enableSerialization)
            await Test("Multiple readers + serialization, 1 mutator", users, withSerialization, true, 
                readerCount, fusionIterationCount / 2);
        await Test("Multiple readers, 1 mutator", users, withoutSerialization, true, 
            readerCount, fusionIterationCount);
        await Test("Single reader, no mutators", users, withoutSerialization, false,
            1, fusionIterationCount * 20);

        Out.WriteLine("Without Stl.Fusion:");
        if (enableSerialization)
            await Test("Multiple readers + serialization, 1 mutator", plainUsers, withSerialization, true,
                readerCount, nonFusionIterationCount);
        await Test("Multiple readers, 1 mutator", plainUsers, withoutSerialization, true,
            readerCount, nonFusionIterationCount);
        await Test("Single reader, no mutators", plainUsers, withoutSerialization, false,
            1, nonFusionIterationCount * 20);
    }

    private async Task Test(string title,
        IUserService users, Action<User> extraAction, bool enableMutations,
        int threadCount, int iterationCount, bool isWarmup = false)
    {
        if (!isWarmup)
            await Test(title, users, extraAction, enableMutations, threadCount, iterationCount / 4, true);

        async Task Mutator(string name, CancellationToken cancellationToken)
        {
            var rnd = new Random();
            var count = 0L;

            while (true) {
                cancellationToken.ThrowIfCancellationRequested();
                var userId = (long) rnd.Next(UserCount);
                // Log.LogDebug($"{name}: R {userId}");
                var user = await users.Get(userId, cancellationToken).ConfigureAwait(false);
                user = user! with { Email = $"{++count}@counter.org" };
                // Log.LogDebug($"{name}: R done, U {user}");
                var updateCommand = new IUserService.UpdateCommand(user);
                await users.UpdateDirectly(updateCommand, cancellationToken).ConfigureAwait(false);

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
                extraAction(user);
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
        WriteLine($"    Operations: {operationCount} ({threadCount} readers x {iterationCount})");

        var startTime = CpuClock.Now;
        var mutatorTask = enableMutations 
            ? Task.Run(() => Mutator("W", stopCts.Token))
            : Task.CompletedTask;
        var tasks = Enumerable
            .Range(0, threadCount)
            .Select(i => Task.Run(() => Reader($"R{i}", iterationCount)))
            .ToArray();
        var results = await Task.WhenAll(tasks);
        var elapsed = CpuClock.Now - startTime;

        stopCts.Cancel();
        await mutatorTask.SilentAwait(false);

        WriteLine($"    Speed:      {operationCount / 1000.0 / elapsed.TotalSeconds:F3} K Ops/sec (took {elapsed.TotalSeconds:F3} sec)");

        results.Length.Should().Be(threadCount);
        results.All(r => r == iterationCount).Should().BeTrue();
    }
}

public class PerformanceTest_Sqlite : PerformanceTestBase
{
    public PerformanceTest_Sqlite(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.Sqlite,
            UseLogging = false,
        })
    { }
}

public class PerformanceTest_PostgreSql : PerformanceTestBase
{
    public PerformanceTest_PostgreSql(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            UseLogging = false,
            DbType = FusionTestDbType.PostgreSql,
        })
    { }
}

public class PerformanceTest_MariaDb : PerformanceTestBase
{
    public PerformanceTest_MariaDb(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            UseLogging = false,
            DbType = FusionTestDbType.MariaDb,
        })
    { }
}

public class PerformanceTest_SqlServer : PerformanceTestBase
{
    public PerformanceTest_SqlServer(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            UseLogging = false,
            DbType = FusionTestDbType.SqlServer,
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
