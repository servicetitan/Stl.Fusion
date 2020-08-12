using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Tests.Services;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class WebSocketHostTest : FusionTestBase
    {
        public WebSocketHostTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

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
    }
}
