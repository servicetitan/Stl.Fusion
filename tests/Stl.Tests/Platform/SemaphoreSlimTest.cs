using Stl.Testing.Collections;

namespace Stl.Tests.Platform;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class SemaphoreSlimTest : TestBase
{
    public SemaphoreSlimTest(ITestOutputHelper @out) : base(@out) { }

    [Fact(Timeout = 5000)]
    public async Task WaitTest1()
    {
        var s = new SemaphoreSlim(0);
        _ = Task.Run(async () => {
            await Task.Delay(TimeSpan.FromSeconds(1));
            s.Release();
        });

        var timestamp = CpuTimestamp.Now;
        Out.WriteLine(timestamp.Elapsed.ToShortString());
        await s.WaitAsync();
        var elapsed = timestamp.Elapsed;
        Out.WriteLine(elapsed.ToShortString());
        elapsed.TotalSeconds.Should().BeGreaterThan(0.9);
    }

    [Fact(Timeout = 5000)]
    public async Task WaitTest2()
    {
        var s = new SemaphoreSlim(0);
        s.CurrentCount.Should().Be(0);
        s.Release();

        var timestamp = CpuTimestamp.Now;
        Out.WriteLine(timestamp.Elapsed.ToShortString());
        await s.WaitAsync();
        var elapsed = timestamp.Elapsed;
        Out.WriteLine(elapsed.ToShortString());
        elapsed.TotalSeconds.Should().BeLessThan(0.5);
    }
}
