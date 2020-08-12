using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Fusion.UI;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class KeyValueModelTest : FusionTestBase
    {
        public KeyValueModelTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public async Task BasicTest()
        {
            var kv = Services.GetRequiredService<IKeyValueService<string>>();
            (await kv.GetValueAsync("")).Should().Be(Option.None<string>());
            await kv.SetValueAsync("", "1");
            (await kv.GetValueAsync("")).Should().Be(Option.Some("1"));

            await using var serving = await WebSocketHost.ServeAsync();
            using var kvm = Services.GetRequiredService<ILiveState<KeyValueModel<string>>>();
            var kvc = Services.GetRequiredService<IStringKeyValueClient>();

            // First read
            var c = kvm.State;
            c.IsConsistent.Should().BeFalse();
            c.Value.Key.Should().Be("");
            c.Value.Value.Should().BeNull();
            c.Value.UpdateCount.Should().Be(0);

            await Task.Delay(300);
            kvm.UpdateError.Should().BeNull();
            c = kvm.State;
            c.IsConsistent.Should().BeTrue();
            c.Value.Key.Should().Be("");
            c.Value.Value.Should().Be("1");
            c.Value.UpdateCount.Should().Be(1);

            // Update
            await kvc.SetValueAsync(kvm.State.Value.Key, "2");
            await Task.Delay(300);
            c = kvm.State;
            c.IsConsistent.Should().BeFalse();
            c.Value.Value.Should().Be("1");
            c.Value.UpdateCount.Should().Be(1);

            await Task.Delay(1000);
            c = kvm.State;
            c.IsConsistent.Should().BeTrue();
            c.Value.Value.Should().Be("2");
            c.Value.UpdateCount.Should().Be(2);
        }
    }
}
