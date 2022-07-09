using Stl.Testing.Collections;

namespace Stl.Tests.Channels;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class EnumerableExtTest : TestBase
{
    public EnumerableExtTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task WithTimeoutTest()
    {
        (await Delays(new [] {0.1d, 0.2, 1})
            .WithItemTimeout(TimeSpan.FromSeconds(0.5))
            .ToResults().Select(r => r.ValueOrDefault)
            .ToArrayAsync()
            ).Should().Equal(0, 1, 0);

        (await Delays(new [] {0.5d, 0.2, 1})
            .WithItemTimeout(TimeSpan.FromSeconds(0.3))
            .ToResults().Select(r => r.ValueOrDefault)
            .ToArrayAsync()
            ).Should().Equal(0);

        (await Delays(new [] {0.5d, 0.2, 0.1, 0.5})
            .WithItemTimeout(TimeSpan.FromSeconds(1), TimeSpan.FromSeconds(0.3))
            .ToResults().Select(r => r.ValueOrDefault)
            .ToArrayAsync()
            ).Should().Equal(0, 1, 2, 0);
    }

    [Fact]
    public async Task TrimOnCancellationTest()
    {
        using var cts1 = new CancellationTokenSource(TimeSpan.FromSeconds(0.1));
        (await Delays(new [] {0.2d, 0.5, 1}, cts1.Token)
            .TrimOnCancellation()
            .ToArrayAsync()
            ).Should().Equal();

        using var cts2 = new CancellationTokenSource(TimeSpan.FromSeconds(0.5));
        (await Delays(new [] {0.1d, 0.2, 1}, cts2.Token)
            .TrimOnCancellation(cts2.Token)
            .ToArrayAsync()
            ).Should().Equal(0, 1);
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
