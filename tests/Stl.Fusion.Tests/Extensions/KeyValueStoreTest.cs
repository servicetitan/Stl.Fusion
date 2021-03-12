using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Extensions;
using Stl.Tests;
using Stl.Time;
using Stl.Time.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Extensions
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class KeyValueStoreTest : FusionTestBase
    {
        public KeyValueStoreTest(ITestOutputHelper @out)
            : base(@out, new FusionTestOptions() { UseTestClock = true })
        { }

        [Fact]
        public async Task BasicTest()
        {
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            await kvs.Set("1", "1v");
            (await kvs.Get("1")).Should().Be("1v");
            await kvs.Remove("1");
            (await kvs.TryGet("1")).Should().Be(null);
        }

        [Fact]
        public async Task ComplexTest()
        {
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            await kvs.Set("1/2", "12");
            (await kvs.CountByPrefix("1")).Should().Be(1);
            (await kvs.ListKeysByPrefix("1", 100)).Length.Should().Be(1);
            (await kvs.CountByPrefix("1/2")).Should().Be(1);
            (await kvs.ListKeysByPrefix("1/2", 100)).Length.Should().Be(1);
            (await kvs.CountByPrefix("1/2/3a")).Should().Be(0);
            (await kvs.ListKeysByPrefix("1/2/3a", 100)).Length.Should().Be(0);

            await kvs.Set("1/2/3a", "123");
            (await kvs.CountByPrefix("1")).Should().Be(2);
            (await kvs.ListKeysByPrefix("1", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefix("1/2")).Should().Be(2);
            (await kvs.ListKeysByPrefix("1/2", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefix("1/2/3a")).Should().Be(1);
            (await kvs.ListKeysByPrefix("1/2/3a", 100)).Length.Should().Be(1);

            await kvs.Set("1/2/3b", "123");
            (await kvs.CountByPrefix("1")).Should().Be(3);
            (await kvs.ListKeysByPrefix("1", 100)).Length.Should().Be(3);
            (await kvs.CountByPrefix("1/2")).Should().Be(3);
            (await kvs.ListKeysByPrefix("1/2", 100)).Length.Should().Be(3);
            (await kvs.CountByPrefix("1/2/3a")).Should().Be(1);
            (await kvs.ListKeysByPrefix("1/2/3a", 100)).Length.Should().Be(1);

            (await kvs.ListKeysByPrefix("1", 3))
                .Should().BeEquivalentTo("1/2", "1/2/3a", "1/2/3b");
            (await kvs.ListKeysByPrefix("1", 2))
                .Should().BeEquivalentTo("1/2", "1/2/3a");
            (await kvs.ListKeysByPrefix("1", PageRef.New(2, "1/2")))
                .Should().BeEquivalentTo("1/2/3a", "1/2/3b");
            (await kvs.ListKeysByPrefix("1", PageRef.New(2, "1/2/3b"), SortDirection.Descending))
                .Should().BeEquivalentTo("1/2/3a", "1/2");
            (await kvs.ListKeysByPrefix("1", PageRef.New(1, "1/2/3b"), SortDirection.Descending))
                .Should().BeEquivalentTo("1/2/3a");
            (await kvs.ListKeysByPrefix("1", PageRef.New(0, "1/2/3b"), SortDirection.Descending))
                .Should().BeEquivalentTo();

            await kvs.RemoveMany(new[] { "1/2/3c", "1/2/3b" });
            (await kvs.CountByPrefix("1")).Should().Be(2);
            (await kvs.ListKeysByPrefix("1", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefix("1/2")).Should().Be(2);
            (await kvs.ListKeysByPrefix("1/2", 100)).Length.Should().Be(2);
            (await kvs.CountByPrefix("1/2/3a")).Should().Be(1);
            (await kvs.ListKeysByPrefix("1/2/3a", 100)).Length.Should().Be(1);

            await kvs.SetMany(new[] {
                ("a/b", "ab", default(Moment?)),
                ("a/c", "ac", default),
            });
            (await kvs.CountByPrefix("1")).Should().Be(2);
            (await kvs.CountByPrefix("a")).Should().Be(2);
            (await kvs.CountByPrefix("")).Should().Be(4);
        }

        [Fact(Skip = "Intermittent failures due to TestClock on this test, to be fixed later.")]
        public async Task TrimmerTest()
        {
            var clock = (TestClock) Services.GetRequiredService<IMomentClock>();
            var kvs = Services.GetRequiredService<IKeyValueStore>();
            await kvs.Set("1", "1v", clock.Now + TimeSpan.FromSeconds(5));
            (await kvs.Get("1")).Should().Be("1v");
            await kvs.Set("2", "2v", clock.Now + TimeSpan.FromMinutes(10));
            (await kvs.Get("2")).Should().Be("2v");
            await kvs.Set("3", "3v");
            (await kvs.Get("3")).Should().Be("3v");
            await kvs.Set("4", "4v", clock.Now + TimeSpan.FromMinutes(4.95));
            (await kvs.Get("4")).Should().Be("4v");

            clock.Settings = new TestClockSettings(TimeSpan.FromMinutes(6));
            await Delay(3); // Let trimmer to kick in
            (await kvs.TryGet("1")).Should().Be(null);
            (await kvs.Get("2")).Should().Be("2v");
            (await kvs.Get("3")).Should().Be("3v");
            (await kvs.TryGet("4")).Should().Be(null);
        }
    }
}
