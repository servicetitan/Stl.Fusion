using System;
using System.Diagnostics;
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
    public class WebServicesTest : FusionTestBase
    {
        public WebServicesTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public async Task TimeServiceClientTest()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            var client = Services.GetRequiredService<ITimeServiceClient>();
            var cTime = await client.GetComputedTimeAsync();
            cTime.IsConsistent.Should().BeTrue();
            (DateTime.Now - cTime.Value).Should().BeLessThan(TimeSpan.FromSeconds(1));

            await Task.Delay(TimeSpan.FromSeconds(2));
            cTime.IsConsistent.Should().BeFalse();
            var time = await cTime.UseAsync();
            (DateTime.Now - time).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ClientTimeServiceTest()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            var service = Services.GetRequiredService<IClientTimeService>();
            var time = await service.GetTimeAsync();
            (DateTime.Now - time).Should().BeLessThan(TimeSpan.FromSeconds(1));

            await Task.Delay(TimeSpan.FromSeconds(2));
            time = await service.GetTimeAsync();
            (DateTime.Now - time).Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ServerTimeModelTest1()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            using var stm = Services.GetRequiredService<ILiveState<ServerTimeModel1>>();

            var c = stm.State;
            c.IsConsistent.Should().BeFalse();
            c.Value.Time.Should().Be(default);

            Debug.WriteLine("0");
            stm.UpdateDelayer.CancelDelays();
            Debug.WriteLine("1");
            await c.UpdateAsync(false);
            Debug.WriteLine("2");

            c = stm.State;
            c.IsConsistent.Should().BeTrue();
            (DateTime.Now - c.Value.Time).Should().BeLessThan(TimeSpan.FromSeconds(1));

            Debug.WriteLine("3");
            await Task.Delay(TimeSpan.FromSeconds(3));
            Debug.WriteLine("4");
            stm.UpdateDelayer.CancelDelays();
            Debug.WriteLine("5");
            await Task.Delay(100); // Let's just wait for the updates to happen
            Debug.WriteLine("6");
            c = stm.State;
            Debug.WriteLine("7");

            // c.IsConsistent.Should().BeTrue(); // Hard to be sure here
            var delta = DateTime.Now - c.Value.Time!.Value;
            Debug.WriteLine(delta.TotalSeconds);
            delta.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task ServerTimeModelTest2()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            using var stm = Services.GetRequiredService<ILiveState<ServerTimeModel2>>();

            var c = stm.State;
            c.IsConsistent.Should().BeFalse();
            c.Value.Time.Should().Be(default);

            Debug.WriteLine("0");
            stm.UpdateDelayer.CancelDelays();
            Debug.WriteLine("1");
            await c.UpdateAsync(false);
            Debug.WriteLine("2");

            c = stm.State;
            c.IsConsistent.Should().BeTrue();
            (DateTime.Now - c.Value.Time).Should().BeLessThan(TimeSpan.FromSeconds(1));

            Debug.WriteLine("3");
            await Task.Delay(TimeSpan.FromSeconds(3));
            Debug.WriteLine("4");
            stm.UpdateDelayer.CancelDelays();
            Debug.WriteLine("5");
            await Task.Delay(100); // Let's just wait for the updates to happen
            Debug.WriteLine("6");
            c = stm.State;
            Debug.WriteLine("7");

            // c.IsConsistent.Should().BeTrue(); // Hard to be sure here
            var delta = DateTime.Now - c.Value.Time!.Value;
            Debug.WriteLine(delta.TotalSeconds);
            delta.Should().BeLessThan(TimeSpan.FromSeconds(1));
        }
    }
}
