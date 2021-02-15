using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.Extensions;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Extensions
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class NestedOperationLoggerTest : FusionTestBase
    {
        public NestedOperationLoggerTest(ITestOutputHelper @out)
            : base(@out, new FusionTestOptions() { UseTestClock = true })
        { }

        [Fact]
        public async Task BasicTest()
        {
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            var c1 = await Computed.CaptureAsync(_ => kvs.TryGetAsync("1"));
            var c2 = await Computed.CaptureAsync(_ => kvs.TryGetAsync("2"));
            var c3 = await Computed.CaptureAsync(_ => kvs.TryGetAsync("3"));
            c1.Value.Should().BeNull();
            c2.Value.Should().BeNull();
            c3.Value.Should().BeNull();

            var commander = Services.Commander();
            var command = new NestedOperationLoggerTester.SetManyCommand(
                new[] {"1", "2", "3"}, "v");
            await commander.CallAsync(command);

            c1.IsInvalidated().Should().BeTrue();
            c2.IsInvalidated().Should().BeTrue();
            c3.IsInvalidated().Should().BeTrue();
            c1 = await c1.UpdateAsync(false);
            c2 = await c2.UpdateAsync(false);
            c3 = await c3.UpdateAsync(false);
            c1.Value.Should().Be("v3");
            c2.Value.Should().Be("v2");
            c3.Value.Should().Be("v1");
        }
    }
}
