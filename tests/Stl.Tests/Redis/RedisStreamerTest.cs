using Stl.Redis;
using Stl.Testing.Collections;

namespace Stl.Tests.Redis;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class RedisStreamerTest : RedisTestBase
{
    public RedisStreamerTest(ITestOutputHelper @out) : base(@out) { }

    [SkipOnGitHubFact]
    public async Task BasicTest()
    {
        var db = GetRedisDb();
        var started = TaskSource.New<Unit>(true);
        var streamer = db.GetStreamer<int>("s");
        await streamer.Remove();
        var streamerCopy = db.GetStreamer<int>("s");

        var writeTask = streamer.Write(
            Delays(new[] {0.1, 0.2, 0.3, 0.1}),
            _ => started.SetResult(default));

        var stream1 = streamer.Read();
        (await streamer.Read().FirstAsync()).Should().Be(0);
        started.Task.IsCompleted.Should().BeTrue();
        var stream2 = streamerCopy.Read();

        (await stream1.ToArrayAsync()).Should().Equal(0, 1, 2, 3);
        (await writeTask.WithTimeout(TimeSpan.FromSeconds(0.05))).Should().BeTrue();

        (await stream2.ToArrayAsync()).Should().Equal(0, 1, 2, 3);
    }

    [SkipOnGitHubFact]
    public async Task CancellationTest()
    {
        var db = GetRedisDb();
        var streamer = db.GetStreamer<int>("s");
        await streamer.Remove();

        using var cts = new CancellationTokenSource(TimeSpan.FromSeconds(0.4));
        var writeTask = streamer.Write(Delays(new[] {0.1, 0.2, 0.3, 0.1}), cts.Token);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => {
            await streamer.Read().ToArrayAsync();
        });
        (await streamer.Read().Take(2).CountAsync()).Should().Be(2);

        await writeTask.SuppressCancellation();
        writeTask.IsCanceled.Should().BeTrue();
    }

    [SkipOnGitHubFact]
    public async Task ExceptionTest1()
    {
        var db = GetRedisDb();
        var streamer = db.GetStreamer<int>("s");
        await streamer.Remove();

        var seq = Delays(new[] {0.1, 0.2, 0.3, 0.1})
            .Concat(Error<int>(new InvalidOperationException()));
        var writeTask = streamer.Write(seq);

        await Assert.ThrowsAsync<InvalidOperationException>(async () => {
            await streamer.Read().ToArrayAsync();
        });
        (await streamer.Read().Take(4).CountAsync()).Should().Be(4);

        await Assert.ThrowsAsync<InvalidOperationException>(
            () => writeTask);
    }

#pragma warning disable CS1998
    async IAsyncEnumerable<T> Error<T>(Exception e)
#pragma warning restore CS1998
    {
        throw e;
#pragma warning disable CS0162
        yield break;
#pragma warning restore CS0162
    }

    async IAsyncEnumerable<int> Delays(
        IEnumerable<double> delays, 
        [EnumeratorCancellation] CancellationToken cancellationToken = default)
    {
        var index = 0;
        foreach (var d in delays) {
            await Task.Delay(TimeSpan.FromSeconds(d), cancellationToken).ConfigureAwait(false);
            yield return index++;
        }
    }
}
