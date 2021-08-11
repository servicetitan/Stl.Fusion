using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.Fusion.Tests.Services;
using Stl.Fusion.Tests.UIModels;
using Stl.Testing;
using Stl.Testing.Collections;
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
            await using var _ = await WebHost.Serve();

            var kv = WebServices.GetRequiredService<IKeyValueService<string>>();
            (await kv.TryGet("")).Should().Be(Option.None<string>());
            (await kv.Get("")).Should().BeNull();
            await kv.Set("", "1");
            (await kv.TryGet("")).Should().Be(Option.Some("1"));
            (await kv.Get("")).Should().Be("1");

            using var kvm = ClientServices.GetRequiredService<IComputedState<KeyValueModel<string>>>();
            var kvc = ClientServices.GetRequiredService<IKeyValueServiceClient<string>>();

            // First read
            var c = kvm.Computed;
            c.IsConsistent().Should().BeFalse();
            c.Value.Key.Should().Be("");
            c.Value.Value.Should().BeNull();
            c.Value.UpdateCount.Should().Be(0);

            await TestEx.WhenMet(() => {
                var snapshot = kvm.Snapshot;
                snapshot.Computed.HasValue.Should().BeTrue();
                var c = snapshot.Computed;
                c.IsConsistent().Should().BeTrue();
                c.Value.Key.Should().Be("");
                c.Value.Value.Should().Be("1");
                c.Value.UpdateCount.Should().Be(1);
            }, TimeSpan.FromSeconds(1));

            // Update
            await kvc.Set(kvm.Computed.Value.Key, "2");
            await Task.Delay(300);
            c = kvm.Computed;
            c.IsConsistent().Should().BeFalse();
            c.Value.Value.Should().Be("1");
            c.Value.UpdateCount.Should().Be(1);

            await Task.Delay(1000);
            c = kvm.Computed;
            c.IsConsistent().Should().BeTrue();
            c.Value.Value.Should().Be("2");
            c.Value.UpdateCount.Should().Be(2);
        }

        [Fact]
        public async Task CommandTest()
        {
            await using var _ = await WebHost.Serve();

            // Server commands
            var kv = WebServices.GetRequiredService<IKeyValueService<string>>();
            (await kv.Get("")).Should().BeNull();

            await kv.SetCmd(new IKeyValueService<string>.SetCommand("", "1"));
            (await kv.Get("")).Should().Be("1");

            await WebServices.Commander().Call(new IKeyValueService<string>.SetCommand("", "2"));
            (await kv.Get("")).Should().Be("2");

            // Client commands
            var kvc = ClientServices.GetRequiredService<IKeyValueServiceClient<string>>();
            (await kv.Get("")).Should().Be("2");

            await kvc.SetCmd(new IKeyValueService<string>.SetCommand("", "1"));
            await Task.Delay(100); // Remote invalidation takes some time
            (await kvc.Get("")).Should().Be("1");

            await ClientServices.Commander().Call(new IKeyValueService<string>.SetCommand("", "2"));
            #if NETCOREAPP
            await Task.Delay(100); // Remote invalidation takes some time
            #else
            await Task.Delay(250); // Remote invalidation takes some time
            #endif
            (await kvc.Get("")).Should().Be("2");
        }

        [Fact]
        public async Task ExceptionTest()
        {
            await using var _ = await WebHost.Serve();
            var kv = WebServices.GetRequiredService<IKeyValueService<string>>();

            try {
                await kv.Get("error");
            }
            catch (ApplicationException ae) {
                ae.Message.Should().Be("Error!");
            }

            var kvc = ClientServices.GetRequiredService<IKeyValueServiceClient<string>>();
            try {
                await kvc.Get("error");
            }
            catch (ApplicationException ae) {
                ae.Message.Should().Be("Error!");
            }
        }
    }
}
