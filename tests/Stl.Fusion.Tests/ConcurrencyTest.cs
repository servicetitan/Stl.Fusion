using Stl.Fusion.Tests.Services;
using Stl.OS;
using Stl.Testing.Collections;

namespace Stl.Fusion.Tests;

[Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
public class ConcurrencyTest : SimpleFusionTestBase
{
    public ConcurrencyTest(ITestOutputHelper @out) : base(@out) { }

    protected override void ConfigureCommonServices(ServiceCollection services)
    {
        var fusion = services.AddFusion();
        fusion.AddComputeService<CounterSumService>();
    }

    [Fact]
    public async Task StateConcurrencyTest()
    {
        const int iterationCount = 10_000;
        var services = CreateServiceProvider();
        var factory = services.StateFactory();

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
            var ms1 = factory.NewMutable(0);
            var ms2 = factory.NewMutable(2);
            var computedStates = Enumerable.Range(0, HardwareInfo.GetProcessorCountFactor(2))
                .Select(_ => factory.NewComputed<int>(
                    updateDelayer,
                    async (_, ct) => {
                        var m1 = await ms1.Use(ct).ConfigureAwait(false);
                        var m2 = await ms2.Use(ct).ConfigureAwait(false);
                        return m1 + m2;
                    }))
                .ToArray();

            async Task Mutator(IMutableState<int> ms) {
                for (var i = 1; i <= iterationCount; i++) {
                    ms.Value = i;
                    if (i % delayFrequency == 0)
                        await Task.Delay(1).ConfigureAwait(false);
                }
            }

            ms1.Value = iterationCount;
            await Task.Run(() => Mutator(ms2));
            ms1.Value.Should().Be(iterationCount);
            ms2.Value.Should().Be(iterationCount);

            foreach (var computedState in computedStates) {
                var snapshot = computedState.Snapshot;
                var c = snapshot.Computed;
                if (!c.IsConsistent()) {
                    await snapshot.WhenUpdated().WaitAsync(TimeSpan.FromSeconds(1));
                    c = computedState.Computed;
                }

                if (c.Value != iterationCount * 2) {
                    Out.WriteLine(computedState.ToString());
                    Out.WriteLine(snapshot.ToString());
                    Out.WriteLine(computedState.Snapshot.ToString());
                    Out.WriteLine(c.ToString());
                    Out.WriteLine(c.Value.ToString());
                    Assert.Fail("One of computed instances has wrong final value!");
                }
            }
        }
    }

    [Fact]
    public async Task AnonymousComputedConcurrencyTest()
    {
        const int iterationCount = 10_000;
        var services = CreateServiceProvider();
        var factory = services.StateFactory();

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
            var ms1 = factory.NewMutable(0);
            var ms2 = factory.NewMutable(2);
            var readers = Enumerable.Range(0, HardwareInfo.GetProcessorCountFactor(2))
                .Select(_ => {
                    var source =  new AnonymousComputedSource<int>(
                        services,
                        async (_, ct) => {
                            var m1 = await ms1.Use(ct).ConfigureAwait(false);
                            var m2 = await ms2.Use(ct).ConfigureAwait(false);
                            return m1 + m2;
                        });
                    var reader = source.Changes(updateDelayer).LastAsync();
                    return (Source: source, Reader: reader);
                })
                .ToArray();

            async Task Mutator(IMutableState<int> ms) {
                for (var i = 1; i <= iterationCount; i++) {
                    ms.Value = i;
                    if (i % delayFrequency == 0)
                        await Task.Delay(1).ConfigureAwait(false);
                }
            }

            ms1.Value = iterationCount;
            await Task.Run(() => Mutator(ms2));
            ms1.Value.Should().Be(iterationCount);
            ms2.Value.Should().Be(iterationCount);

            foreach (var reader in readers) {
                var source = reader.Source;
                var c = (Computed<int>)source.Computed;
                if (!c.IsConsistent())
                    c = await c.Update().AsTask().WaitAsync(TimeSpan.FromSeconds(1));

                if (c.Value != iterationCount * 2) {
                    Out.WriteLine(source.ToString());
                    Out.WriteLine(c.ToString());
                    Out.WriteLine(c.Value.ToString());
                    Assert.Fail("One of computed instances has wrong final value!");
                }
            }
        }
    }

    [Fact]
    public async Task ComputedConcurrencyTest()
    {
        const int iterationCount = 10_000;
        var services = CreateServiceProvider();
        var counterSum = services.GetRequiredService<CounterSumService>();

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
            var readers = (await Enumerable.Range(0, HardwareInfo.GetProcessorCountFactor())
                .Select(async _ => {
                    var computed = await Computed.Capture(() => counterSum.Sum(0, 1));
                    var reader = computed.Changes(updateDelayer).LastAsync();
                    return (Computed: computed, Reader: reader);
                })
                .Collect()
                ).ToArray();

            async Task Mutator(IMutableState<int> ms) {
                for (var i = 1; i <= iterationCount; i++) {
                    ms.Value = i;
                    if (i % delayFrequency == 0)
                        await Task.Delay(1).ConfigureAwait(false);
                }
            }

            const int counterCount = 2;
            for (var usedCounterIndex = 0; usedCounterIndex < counterCount; usedCounterIndex++) {
                for (var i = 0; i < counterCount; i++) {
                    counterSum[i].Value = iterationCount;
                    counterSum[0].Value.Should().Be(iterationCount);
                }

                counterSum[usedCounterIndex].Value = 0;
                counterSum[usedCounterIndex].Value.Should().Be(0);
                await Task.Run(() => Mutator(counterSum[usedCounterIndex]));
                counterSum[usedCounterIndex].Value.Should().Be(iterationCount);

                var expectedValue = iterationCount * 2;
                foreach (var reader in readers) {
                    var c = reader.Computed;
                    if (c.Value != expectedValue) {
                        await c.WhenInvalidated().WaitAsync(TimeSpan.FromSeconds(0.5));
                        c = await c.Update();
                    }
                    if (c.Value != expectedValue) {
                        Out.WriteLine(c.ToString());
                        Out.WriteLine(c.Value.ToString());
                        Assert.Fail("One of computed instances has wrong final value!");
                    }
                }
            }
        }
    }
}
