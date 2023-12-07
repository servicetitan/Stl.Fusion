using Stl.Testing.Collections;
using Stl.Time.Internal;

namespace Stl.Tests.Platform;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class TaskDelayTest(ITestOutputHelper @out) : TestBase(@out)
{
    [Fact]
    public async Task DelayTest()
    {
        var tasks = new List<Task<TimeSpan>>();
        var requestedDelay = TimeSpan.FromMilliseconds(5);
        var sw = new SpinWait();
        for (var iteration = 0; iteration < 10; iteration++) {
            var now = CpuTimestamp.Now;
            var initialDelay = TimeSpan.FromMilliseconds(new Random().NextDouble() * 3);
            while (now.Elapsed < initialDelay)
                sw.SpinOnce();

            for (var i = 0; i < 17; i++) {
                tasks.Add(MeasureDelay(requestedDelay));
                now = CpuTimestamp.Now;
                while (now.Elapsed < TimeSpan.FromMilliseconds(1))
                    sw.SpinOnce();
            }
            await Task.WhenAll(tasks).ConfigureAwait(false);
            var minDelay = tasks.Select(t => t.Result).Min();
            var maxDelay = tasks.Select(t => t.Result).Max();
            Out.WriteLine($"Delays: min = {minDelay.TotalMilliseconds}ms, max = {maxDelay.TotalMilliseconds}ms");
            minDelay.Should().BeGreaterThanOrEqualTo(requestedDelay);
        }
    }

    private async Task<TimeSpan> MeasureDelay(TimeSpan delay)
    {
        var now = CpuTimestamp.Now;
        await Task.Delay(delay).ConfigureAwait(false);
        return now.Elapsed;
    }
}
