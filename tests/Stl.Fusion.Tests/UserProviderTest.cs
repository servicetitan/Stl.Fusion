using Stl.Fusion.Internal;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;

namespace Stl.Fusion.Tests;

public class UserProviderTest(ITestOutputHelper @out) : FusionTestBase(@out)
{
    [Fact]
    public async Task InvalidateEverythingTest()
    {
        var commander = Services.Commander();
        var users = Services.GetRequiredService<IUserService>();
        // We need at least 1 user to see count invalidation messages
        await commander.Call(new UserService_Add(new User() {
            Id = int.MaxValue,
            Name = "Chuck Norris",
        }));

        var u1 = await users.Get(int.MaxValue);
        var c1 = await Computed.Capture(() => users.Count());

        await users.Invalidate();

        var u2 = await users.Get(int.MaxValue);
        var c2 = await Computed.Capture(() => users.Count());

        u2.Should().NotBeSameAs(u1);
        u2!.Id.Should().Be(u1!.Id);
        u2.Name.Should().Be(u1.Name);

        c2.Should().NotBeSameAs(c1);
        c2!.Value.Should().Be(c1!.Value);
    }

    [Fact]
    public async Task InvalidationTest()
    {
        var commander = Services.Commander();
        var users = Services.GetRequiredService<IUserService>();
        // We need at least 1 user to see count invalidation messages
        await commander.Call(new UserService_Add(new User() {
            Id = int.MaxValue,
            Name = "Chuck Norris",
        }));
        var userCount = await users.Count();

        var u = new User() {
            Id = 1000,
            Name = "Bruce Lee"
        };
        // This delete won't do anything, since the user doesn't exist
        (await commander.Call(new UserService_Delete(u))).Should().BeFalse();
        // Thus count shouldn't change
        (await users.Count()).Should().Be(userCount);
        // But after this line the could should change
        await commander.Call(new UserService_Add(u));

        var u1 = await users.Get(u.Id);
        u1.Should().NotBeNull();
        u1.Should().NotBeSameAs(u); // Because it's fetched
        u1!.Id.Should().Be(u.Id);
        u1.Name.Should().Be(u.Name);
        (await users.Count()).Should().Be(++userCount);

        var u2 = await users.Get(u.Id);
        u2.Should().BeSameAs(u1);

        u = u with { Name = "Jackie Chan" };
        await commander.Call(new UserService_Update(u)); // u.Name change

        var u3 = await users.Get(u.Id);
        u3.Should().NotBeNull();
        u3.Should().NotBeSameAs(u2);
        u3!.Id.Should().Be(u.Id);
        u3.Name.Should().Be(u.Name);
        (await users.Count()).Should().Be(userCount);
    }

    [Fact]
    public async Task StandaloneComputedTest()
    {
        var commander = Services.Commander();
        var stateFactory = Services.StateFactory();
        var users = Services.GetRequiredService<IUserService>();
        var time = Services.GetRequiredService<ITimeService>();

        var u = new User() {
            Id = int.MaxValue,
            Name = "Chuck Norris",
        };
        await commander.Call(new UserService_Add(u));

        using var sText = await stateFactory.NewComputed<string>(
            FixedDelayer.Instant,
            async (s, cancellationToken) => {
                var norris = await users.Get(int.MaxValue, cancellationToken).ConfigureAwait(false);
                var now = await time.GetTime().ConfigureAwait(false);
                return $"@ {now:hh:mm:ss.fff}: {norris?.Name ?? "(none)"}";
            }).Update();
        sText.Updated += (s, _) => Log?.LogInformation($"{s.Value}");

        for (var i = 1; i <= 10; i += 1) {
            u = u with { Name = $"Chuck Norris Lvl{i}" };
            await commander.Call(new UserService_Add(u, true));
            await Task.Delay(100);
        }

        var text = await sText.Use();
        text.Should().EndWith("Lvl10");
    }

    [Fact]
    public async Task SuppressTest()
    {
        var stateFactory = Services.StateFactory();
        var time = Services.GetRequiredService<ITimeService>();
        var count1 = 0;
        var count2 = 0;

#pragma warning disable 1998
        var s1 = await stateFactory.NewComputed<int>(
            FixedDelayer.Instant,
            async (s, ct) => count1++).Update();
        var s2 = await stateFactory.NewComputed<int>(
            FixedDelayer.Instant,
            async (s, ct) => count2++).Update();
#pragma warning restore 1998
        var s12 = await stateFactory.NewComputed<(int, int)>(
            FixedDelayer.Instant,
            async (s, cancellationToken) => {
                var a = await s1.Use(cancellationToken);
                using var _ = Computed.SuspendDependencyCapture();
                var b = await s2.Use(cancellationToken);
                return (a, b);
            }).Update();

        var v12a = await s12.Use();
        s1.Computed.Invalidate(); // Should increment c1 & impact c12
        var v12b = await s12.Use();
        v12b.Should().Be((v12a.Item1 + 1, v12a.Item2));
        s2.Computed.Invalidate(); // Should increment c2, but shouldn't impact c12
        var v12c = await s12.Use();
        v12c.Should().Be(v12b);
    }

    [Fact]
    public void KeepAliveSlotTest()
    {
        var q = Timeouts.KeepAliveQuanta;
        q.TotalSeconds.Should().BeGreaterThan(0.2);
        q.TotalSeconds.Should().BeLessThan(0.21);

        Timeouts.GetKeepAliveSlot(Timeouts.StartedAt).Should().Be(0L);
        Timeouts.GetKeepAliveSlot(Timeouts.StartedAt + q).Should().Be(1L);
        Timeouts.GetKeepAliveSlot(Timeouts.StartedAt + q.Multiply(2)).Should().Be(2L);
    }

    [Fact]
    public async Task KeepAliveTimeTest()
    {
        var users = Services.GetRequiredService<IUserService>();

        var cUser0 = await Computed.Capture(() => users.Get(0));
        var cCount = await Computed.Capture(() => users.Count());

        cUser0!.Options.MinCacheDuration.Should().Be(TimeSpan.FromSeconds(60));
        cCount!.Options.MinCacheDuration.Should().Be(TimeSpan.FromSeconds(60));
    }

    [Fact]
    public async Task MultiHostInvalidationTest()
    {
        var users = Services.GetRequiredService<IUserService>();
        await using var _ = await WebHost.Serve();
        var webUsers = WebServices.GetRequiredService<IUserService>();

        async Task PingPong(IUserService users1, IUserService users2, User user)
        {
            var commander = users1.GetCommander();
            var count0 = await users1.Count();
            (await users2.Count()).Should().Be(count0);

            await commander.Call(new UserService_Add(user));
            (await users1.Count()).Should().Be(++count0);

            await Delay(0.5);

            var user2 = await users2.Get(user.Id);
            user2.Should().NotBeNull();
            user2!.Id.Should().Be(user.Id);
            (await users2.Count()).Should().Be(count0);
        }

        for (var i = 0; i < 5; i++) {
            var id1 = i * 2;
            var id2 = id1 + 1;
            Out.WriteLine($"{i}: ping...");
            await PingPong(users, webUsers, new User() { Id = id1, Name = id1.ToString()});
            Out.WriteLine($"{i}: pong...");
            await PingPong(users, webUsers, new User() { Id = id2, Name = id2.ToString()});
            // await PingPong(webUsers, users, new User() { Id = id2, Name = id2.ToString()});
        }
    }
}
