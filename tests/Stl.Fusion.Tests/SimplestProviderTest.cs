using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class SimplestProviderTest : FusionTestBase
{
    public SimplestProviderTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var p = Services.GetRequiredService<ISimplestProvider>();
        p.SetValue("");
        var (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);

        (await p.GetValue()).Should().Be("");
        (await p.GetCharCount()).Should().Be(0);
        p.GetValueCallCount.Should().Be(++gv);
        p.GetCharCountCallCount.Should().Be(++gcc);

        p.SetValue("1");
        (await p.GetValue()).Should().Be("1");
        (await p.GetCharCount()).Should().Be(1);
        p.GetValueCallCount.Should().Be(++gv);
        p.GetCharCountCallCount.Should().Be(++gcc);

        // Retrying the same - call counts shouldn't change
        (await p.GetValue()).Should().Be("1");
        (await p.GetCharCount()).Should().Be(1);
        p.GetValueCallCount.Should().Be(gv);
        p.GetCharCountCallCount.Should().Be(gcc);
    }

    [Fact]
    public async Task ScopedTest()
    {
        var p = Services.GetRequiredService<ISimplestProvider>();
        p.SetValue("");
        var (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);
        (await p.GetValue()).Should().Be("");
        (await p.GetCharCount()).Should().Be(0);
        p.GetValueCallCount.Should().Be(++gv);
        p.GetCharCountCallCount.Should().Be(++gcc);

        using (var s1 = Services.CreateScope()) {
            p = s1.ServiceProvider.GetRequiredService<ISimplestProvider>();
            (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);
            (await p.GetValue()).Should().Be("");
            (await p.GetCharCount()).Should().Be(0);
            p.GetValueCallCount.Should().Be(gv);
            p.GetCharCountCallCount.Should().Be(gcc);
        }
        using (var s2 = Services.CreateScope()) {
            p = s2.ServiceProvider.GetRequiredService<ISimplestProvider>();
            (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);
            (await p.GetValue()).Should().Be("");
            (await p.GetCharCount()).Should().Be(0);
            p.GetValueCallCount.Should().Be(gv);
            p.GetCharCountCallCount.Should().Be(gcc);
        }
    }

    [Fact]
    public async Task ExceptionCachingTest()
    {
        var p = Services.GetRequiredService<ISimplestProvider>();
        p.SetValue("");
        var (gv, gcc) = (p.GetValueCallCount, p.GetCharCountCallCount);

        p.SetValue(null!); // Will cause an exception in GetCharCount
        (await p.GetValue()).Should().Be(null);
        p.GetValueCallCount.Should().Be(++gv);
        p.GetCharCountCallCount.Should().Be(gcc);

        await Assert.ThrowsAsync<NullReferenceException>(() => p.GetCharCount());
        p.GetValueCallCount.Should().Be(gv);
        p.GetCharCountCallCount.Should().Be(++gcc);

        // Exceptions are also cached, so counts shouldn't change here
        await Assert.ThrowsAsync<NullReferenceException>(() => p.GetCharCount());
        p.GetValueCallCount.Should().Be(gv);
        p.GetCharCountCallCount.Should().Be(gcc);

        // But if we wait for 0.3s+, it should recompute again
        await Task.Delay(1100);
        await Assert.ThrowsAsync<NullReferenceException>(() => p.GetCharCount());
        p.GetValueCallCount.Should().Be(gv);
        p.GetCharCountCallCount.Should().Be(++gcc);
    }

    [Fact]
    public async Task ExceptionCaptureTest()
    {
        var p = Services.GetRequiredService<ISimplestProvider>();
        p.SetValue(null!); // Will cause an exception in GetCharCount
        var c1Opt = await Computed.TryCapture(_ => p.GetCharCount());
        var c2 = await Computed.Capture(_ => p.GetCharCount());
        c1Opt.Value.Error!.GetType().Should().Be(typeof(NullReferenceException));
        c2.Should().BeSameAs(c1Opt.Value);
    }

    [Fact]
    public async Task OptionsTest()
    {
        var d = ComputedOptions.Default;
        var p = Services.GetRequiredService<ISimplestProvider>();
        p.SetValue("");

        var c1 = await Computed.Capture(_ => p.GetValue());
        c1.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(10));
        c1.Options.ErrorAutoInvalidateTime.Should().Be(d.ErrorAutoInvalidateTime);
        c1.Options.AutoInvalidateTime.Should().Be(d.AutoInvalidateTime);

        var c2 = await Computed.Capture(_ => p.GetCharCount());
        c2.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(0.5));
        c2.Options.ErrorAutoInvalidateTime.Should().Be(TimeSpan.FromSeconds(0.5));
        c2.Options.AutoInvalidateTime.Should().Be(d.AutoInvalidateTime);
    }

    [Fact]
    public async Task CommandTest()
    {
        var p = Services.GetRequiredService<ISimplestProvider>();
        await Services.Commander().Run(new SetValueCommand() { Value = "1" });
        (await p.GetValue()).Should().Be("1");
        await Services.Commander().Run(new SetValueCommand() { Value = "2" });
        (await p.GetValue()).Should().Be("2");
    }
}
