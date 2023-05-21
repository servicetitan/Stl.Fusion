namespace Stl.Fusion.Tests;

public class AnonymousComputedTest : SimpleFusionTestBase
{
    public AnonymousComputedTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task BasicTest()
    {
        var services = CreateServices();

        var id = 0;
        var ci = new AnonymousComputedSource<int>(services,
            (_, _) => {
                var value = Interlocked.Increment(ref id);
                Out.WriteLine($"Computed: {value}");
                return new ValueTask<int>(value);
            });

        ci.IsComputed.Should().BeFalse();
        Assert.Throws<InvalidOperationException>(() => ci.Computed);

        (await ci.Use()).Should().Be(1);
        ci.IsComputed.Should().BeTrue();
        (await ci.Use()).Should().Be(1);
        ci.Computed.Value.Should().Be(1);
        (await ci.Computed.Use()).Should().Be(1);

        ci.Computed.Invalidate();

        (await ci.Use()).Should().Be(2);
        (await ci.Use()).Should().Be(2);
        ci.Computed.Value.Should().Be(2);
        (await ci.Computed.Use()).Should().Be(2);
    }

    [Fact]
    public async Task ComputedOptionsTest()
    {
        var services = CreateServices();

        var id = 0;
        var ci = new AnonymousComputedSource<int>(services,
            (_, _) => {
                var value = Interlocked.Increment(ref id);
                Out.WriteLine($"Computed: {value}");
                return new ValueTask<int>(value);
            }) {
            ComputedOptions = new() {
                AutoInvalidationDelay = TimeSpan.FromSeconds(0.2),
            }
        };
        ci.IsComputed.Should().BeFalse();

        (await ci.Use()).Should().Be(1);
        ci.IsComputed.Should().BeTrue();

        await ci.When(x => x > 1).WaitAsync(TimeSpan.FromSeconds(1));
        await ci.Changes().Take(3).CountAsync().AsTask().WaitAsync(TimeSpan.FromSeconds(2));
        (await ci.Use()).Should().BeGreaterThan(3);
    }
}
