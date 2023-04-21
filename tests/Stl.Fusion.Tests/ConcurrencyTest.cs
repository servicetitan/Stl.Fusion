using Stl.OS;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class ConcurrencyTest : SimpleFusionTestBase
{
    public ConcurrencyTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureCommonServices(ServiceCollection services) { }

    [Fact]
    public async Task StateConcurrencyTest()
    {
        const int iterationCount = 10_000;
        var factory = CreateServiceProvider().StateFactory();

        var updateDelayer = FixedDelayer.ZeroUnsafe;
        await Test(50);
        await Test(1000);
        updateDelayer = FixedDelayer.Instant;
        await Test(50);
        await Test(1000);
        updateDelayer = FixedDelayer.Get(0.1);
        await Test(50);
        await Test(1000);

        async Task Test(int delayFrequency)
        {
            var mutableState = factory.NewMutable(0);
            var computedStates = Enumerable.Range(0, HardwareInfo.GetProcessorCountFactor(2))
                .Select(_ => factory.NewComputed<int>(
                    updateDelayer,
                    async (_, ct) => {
                        var result = await mutableState.Use(ct).ConfigureAwait(false);
                        return result;
                    }))
                .ToArray();

            var mutator = Task.Run(async () => {
                for (var i = 1; i <= iterationCount; i++) {
                    mutableState.Value = i;
                    if (i % delayFrequency == 0)
                        await Task.Delay(1).ConfigureAwait(false);
                }
            });
            await mutator;
            mutableState.Value.Should().Be(iterationCount);

            foreach (var computedState in computedStates) {
                var snapshot = computedState.Snapshot;
                var computed = snapshot.Computed;
                if (!computed.IsConsistent()) {
                    await snapshot.WhenUpdated().WaitAsync(TimeSpan.FromSeconds(1));
                    computed = computedState.Computed;
                }

                if (computed.Value != iterationCount) {
                    Out.WriteLine(computedState.ToString());
                    Out.WriteLine(snapshot.ToString());
                    Out.WriteLine(computedState.Snapshot.ToString());
                    Out.WriteLine(computed.ToString());
                    Out.WriteLine(computed.Value.ToString());
                    Assert.Fail("One of computed instances has wrong final value!");
                }
            }
        }
    }
}
