using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Extensions;
using Stl.Time;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Extensions
{
    public class KeyValueStoreTest : FusionTestBase
    {
        public KeyValueStoreTest(ITestOutputHelper @out)
            : base(@out, new FusionTestOptions() { UseTestClock = true })
        { }

        [Fact]
        public async Task BasicTest()
        {
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            await kvs.SetAsync("1", "1v");
            (await kvs.GetAsync("1")).Should().Be("1v");
            await kvs.RemoveAsync("1");
            (await kvs.TryGetAsync("1")).Should().Be(null);
        }

        [Fact]
        public async Task PrefixTest()
        {
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            await kvs.SetAsync("1/2", "12");
            (await kvs.CountByPrefixAsync("1")).Should().Be(1);
            (await kvs.ListKeysByPrefixAsync("1", 100)).Length.Should().Be(1);
            (await kvs.CountByPrefixAsync("1/2")).Should().Be(1);
            (await kvs.ListKeysByPrefixAsync("1/2", 100)).Length.Should().Be(1);
            (await kvs.CountByPrefixAsync("1/2/3a")).Should().Be(0);
            (await kvs.ListKeysByPrefixAsync("1/2/3a", 100)).Length.Should().Be(0);

            await kvs.SetAsync("1/2/3a", "123");
            (await kvs.CountByPrefixAsync("1")).Should().Be(2);
            (await kvs.ListKeysByPrefixAsync("1", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefixAsync("1/2")).Should().Be(2);
            (await kvs.ListKeysByPrefixAsync("1/2", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefixAsync("1/2/3a")).Should().Be(1);
            (await kvs.ListKeysByPrefixAsync("1/2/3a", 100)).Length.Should().Be(1);

            await kvs.SetAsync("1/2/3b", "123");
            (await kvs.CountByPrefixAsync("1")).Should().Be(3);
            (await kvs.ListKeysByPrefixAsync("1", 100)).Length.Should().Be(3);
            (await kvs.CountByPrefixAsync("1/2")).Should().Be(3);
            (await kvs.ListKeysByPrefixAsync("1/2", 100)).Length.Should().Be(3);
            (await kvs.CountByPrefixAsync("1/2/3a")).Should().Be(1);
            (await kvs.ListKeysByPrefixAsync("1/2/3a", 100)).Length.Should().Be(1);

            await kvs.RemoveAsync(new[] { "1/2/3c", "1/2/3b" });
            (await kvs.CountByPrefixAsync("1")).Should().Be(2);
            (await kvs.ListKeysByPrefixAsync("1", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefixAsync("1/2")).Should().Be(2);
            (await kvs.ListKeysByPrefixAsync("1/2", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefixAsync("1/2/3a")).Should().Be(1);
            (await kvs.ListKeysByPrefixAsync("1/2/3a", 100)).Length.Should().Be(1);
        }

        [Fact(Skip = "Intermittent failures due to TestClock on this test, to be fixed later.")]
        public async Task TrimmerTest()
        {
            var clock = (TestClock) Services.GetRequiredService<IMomentClock>();
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            await kvs.SetAsync("1", "1v", clock.Now + TimeSpan.FromSeconds(5));
            (await kvs.GetAsync("1")).Should().Be("1v");
            await kvs.SetAsync("2", "2v", clock.Now + TimeSpan.FromMinutes(10));
            (await kvs.GetAsync("2")).Should().Be("2v");
            await kvs.SetAsync("3", "3v");
            (await kvs.GetAsync("3")).Should().Be("3v");
            await kvs.SetAsync("4", "4v", clock.Now + TimeSpan.FromMinutes(4.95));
            (await kvs.GetAsync("4")).Should().Be("4v");

            clock.Settings = new TestClockSettings(TimeSpan.FromMinutes(6));
            await DelayAsync(3); // Let trimmer to kick in
            (await kvs.TryGetAsync("1")).Should().Be(null);
            (await kvs.GetAsync("2")).Should().Be("2v");
            (await kvs.GetAsync("3")).Should().Be("3v");
            (await kvs.TryGetAsync("4")).Should().Be(null);
        }
    }
}
