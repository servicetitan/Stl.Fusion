using Stl.Fusion.Extensions;
using Stl.Multitenancy;
using Stl.Time.Testing;

namespace Stl.Fusion.Tests.Extensions;

public class InMemoryKeyValueStoreTest : KeyValueStoreTestBase
{
    public InMemoryKeyValueStoreTest(ITestOutputHelper @out) : base(@out, true) { }
}

public class DbKeyValueStoreTest : KeyValueStoreTestBase
{
    public DbKeyValueStoreTest(ITestOutputHelper @out) : base(@out, false) { }
}

public abstract class KeyValueStoreTestBase : FusionTestBase
{
    protected KeyValueStoreTestBase(ITestOutputHelper @out, bool useInMemoryKeyValueStore)
        : base(@out, new FusionTestOptions() {
            UseTestClock = true,
            UseInMemoryKeyValueStore = useInMemoryKeyValueStore,
        })
    { }

    [Fact]
    public async Task BasicTest()
    {
        var kvs = Services.GetRequiredService<IKeyValueStore>();
        var tenantId = Tenant.Default.Id;

        await kvs.Set(tenantId, "1", "1v");
        (await kvs.Get(tenantId, "1")).Should().Be("1v");
        await kvs.Remove(tenantId, "1");
        (await kvs.Get(tenantId, "1")).Should().Be(null);
    }

    [Fact]
    public async Task ComplexTest()
    {
        var kvs = Services.GetRequiredService<IKeyValueStore>();
        var tenantId = Tenant.Default.Id;

        await kvs.Set(tenantId, "1/2", "12");
        (await kvs.Count(tenantId, "1")).Should().Be(1);
        (await kvs.ListKeySuffixes(tenantId, "1", 100)).Length.Should().Be(1);
        (await kvs.Count(tenantId, "1/2")).Should().Be(1);
        (await kvs.ListKeySuffixes(tenantId, "1/2", 100)).Length.Should().Be(1);
        (await kvs.Count(tenantId, "1/2/3a")).Should().Be(0);
        (await kvs.ListKeySuffixes(tenantId, "1/2/3a", 100)).Length.Should().Be(0);

        await kvs.Set(tenantId, "1/2/3a", "123");
        (await kvs.Count(tenantId, "1")).Should().Be(2);
        (await kvs.ListKeySuffixes(tenantId, "1", 100)).Length.Should().Be(2);
        (await kvs.Count(tenantId, "1/2")).Should().Be(2);
        (await kvs.ListKeySuffixes(tenantId, "1/2", 100)).Length.Should().Be(2);
        (await kvs.Count(tenantId, "1/2/3a")).Should().Be(1);
        (await kvs.ListKeySuffixes(tenantId, "1/2/3a", 100)).Length.Should().Be(1);

        await kvs.Set(tenantId, "1/2/3b", "123");
        (await kvs.Count(tenantId, "1")).Should().Be(3);
        (await kvs.ListKeySuffixes(tenantId, "1", 100)).Length.Should().Be(3);
        (await kvs.Count(tenantId, "1/2")).Should().Be(3);
        (await kvs.ListKeySuffixes(tenantId, "1/2", 100)).Length.Should().Be(3);
        (await kvs.Count(tenantId, "1/2/3a")).Should().Be(1);
        (await kvs.ListKeySuffixes(tenantId, "1/2/3a", 100)).Length.Should().Be(1);

        (await kvs.ListKeySuffixes(tenantId, "1", 3))
            .Should().BeEquivalentTo("/2", "/2/3a", "/2/3b");
        (await kvs.ListKeySuffixes(tenantId, "1", 2))
            .Should().BeEquivalentTo("/2", "/2/3a");
        (await kvs.ListKeySuffixes(tenantId, "1", PageRef.New(2, "1/2")))
            .Should().BeEquivalentTo("/2/3a", "/2/3b");
        (await kvs.ListKeySuffixes(tenantId, "1", PageRef.New(2, "1/2/3b"), SortDirection.Descending))
            .Should().BeEquivalentTo("/2/3a", "/2");
        (await kvs.ListKeySuffixes(tenantId, "1", PageRef.New(1, "1/2/3b"), SortDirection.Descending))
            .Should().BeEquivalentTo("/2/3a");
        (await kvs.ListKeySuffixes(tenantId, "1", PageRef.New(0, "1/2/3b"), SortDirection.Descending))
            .Should().BeEquivalentTo();

        await kvs.Remove(tenantId, new[] { "1/2/3c", "1/2/3b" });
        (await kvs.Count(tenantId, "1")).Should().Be(2);
        (await kvs.ListKeySuffixes(tenantId, "1", 100)).Length.Should().Be(2);
        (await kvs.Count(tenantId, "1/2")).Should().Be(2);
        (await kvs.ListKeySuffixes(tenantId, "1/2", 100)).Length.Should().Be(2);
        (await kvs.Count(tenantId, "1/2/3a")).Should().Be(1);
        (await kvs.ListKeySuffixes(tenantId, "1/2/3a", 100)).Length.Should().Be(1);

        await kvs.Set(tenantId, new[] {
            ("a/b", "ab", default(Moment?)),
            ("a/c", "ac", default),
        });
        (await kvs.Count(tenantId, "1")).Should().Be(2);
        (await kvs.Count(tenantId, "a")).Should().Be(2);
        (await kvs.Count(tenantId, "")).Should().Be(4);
    }

    [Fact]
    public async Task ExpirationTest()
    {
        var kvs = Services.GetRequiredService<IKeyValueStore>();
        var clock = (TestClock) Services.GetRequiredService<IMomentClock>();
        var tenantId = Tenant.Default.Id;

        await kvs.Set(tenantId, "1", "1v", clock.Now + TimeSpan.FromSeconds(5));
        (await kvs.Get(tenantId, "1")).Should().Be("1v");
        await kvs.Set(tenantId, "2", "2v", clock.Now + TimeSpan.FromMinutes(10));
        (await kvs.Get(tenantId, "2")).Should().Be("2v");
        await kvs.Set(tenantId, "3", "3v");
        (await kvs.Get(tenantId, "3")).Should().Be("3v");
        await kvs.Set(tenantId, "4", "4v", clock.Now + TimeSpan.FromMinutes(4.95));
        (await kvs.Get(tenantId, "4")).Should().Be("4v");

        clock.Settings = new TestClockSettings(TimeSpan.FromMinutes(6));
        await Delay(3); // Let trimmer to kick in
        ComputedRegistry.Instance.InvalidateEverything();

        (await kvs.Get(tenantId, "1")).Should().Be(null);
        (await kvs.Get(tenantId, "2")).Should().Be("2v");
        (await kvs.Get(tenantId, "3")).Should().Be("3v");
        (await kvs.Get(tenantId, "4")).Should().Be(null);
    }
}
