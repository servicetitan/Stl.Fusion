using System;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Stl.Fusion.Tests.Model;
using Stl.Fusion.Tests.Services;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class UserProviderTest : FusionTestBase
    {
        public UserProviderTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task InvalidateEverythingTest()
        {
            var users = Services.GetRequiredService<IUserService>();
            // We need at least 1 user to see count invalidation messages
            await users.Create(new(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }));

            var u1 = await users.TryGet(int.MaxValue);
            var c1 = await Computed.Capture(_ => users.Count());

            users.Invalidate();

            var u2 = await users.TryGet(int.MaxValue);
            var c2 = await Computed.Capture(_ => users.Count());

            u2.Should().NotBeSameAs(u1);
            u2!.Id.Should().Be(u1!.Id);
            u2.Name.Should().Be(u1.Name);

            c2.Should().NotBeSameAs(c1);
            c2!.Value.Should().Be(c1!.Value);
        }

        [Fact]
        public async Task InvalidationTest()
        {
            var users = Services.GetRequiredService<IUserService>();
            // We need at least 1 user to see count invalidation messages
            await users.Create(new(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }));
            var userCount = await users.Count();

            var u = new User() {
                Id = 1000,
                Name = "Bruce Lee"
            };
            // This delete won't do anything, since the user doesn't exist
            (await users.Delete(new(u))).Should().BeFalse();
            // Thus count shouldn't change
            (await users.Count()).Should().Be(userCount);
            // But after this line the could should change
            await users.Create(new(u));

            var u1 = await users.TryGet(u.Id);
            u1.Should().NotBeNull();
            u1.Should().NotBeSameAs(u); // Because it's fetched
            u1!.Id.Should().Be(u.Id);
            u1.Name.Should().Be(u.Name);
            (await users.Count()).Should().Be(++userCount);

            var u2 = await users.TryGet(u.Id);
            u2.Should().BeSameAs(u1);

            u = u with { Name = "Jackie Chan" };
            await users.Update(new(u)); // u.Name change

            var u3 = await users.TryGet(u.Id);
            u3.Should().NotBeNull();
            u3.Should().NotBeSameAs(u2);
            u3!.Id.Should().Be(u.Id);
            u3.Name.Should().Be(u.Name);
            (await users.Count()).Should().Be(userCount);
        }

        [Fact]
        public async Task StandaloneComputedTest()
        {
            var stateFactory = Services.StateFactory();
            var users = Services.GetRequiredService<IUserService>();
            var time = Services.GetRequiredService<ITimeService>();

            var u = new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            };
            await users.Create(new(u));

            using var sText = await stateFactory.NewComputed<string>(
                UpdateDelayer.ZeroUpdateDelay,
                async (s, cancellationToken) => {
                    var norris = await users.TryGet(int.MaxValue, cancellationToken).ConfigureAwait(false);
                    var now = await time.GetTime().ConfigureAwait(false);
                    return $"@ {now:hh:mm:ss.fff}: {norris?.Name ?? "(none)"}";
                }).Update();
            sText.Updated += (s, _) => Log.LogInformation($"{s.Value}");

            for (var i = 1; i <= 10; i += 1) {
                u = u with { Name = $"Chuck Norris Lvl{i}" };
                await users.Create(new(u, true));
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
                UpdateDelayer.ZeroUpdateDelay,
                async (s, ct) => count1++).Update();
            var s2 = await stateFactory.NewComputed<int>(
                UpdateDelayer.ZeroUpdateDelay,
                async (s, ct) => count2++).Update();
#pragma warning restore 1998
            var s12 = await stateFactory.NewComputed<(int, int)>(
                UpdateDelayer.ZeroUpdateDelay,
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
        public async Task KeepAliveTimeTest()
        {
            var users = Services.GetRequiredService<IUserService>();

            var cUser0 = await Computed.Capture(_ => users.TryGet(0));
            var cCount = await Computed.Capture(_ => users.Count());

            cUser0!.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(1));
            cCount!.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(1));
        }

        [Fact]
        public async Task MultiHostInvalidationTest()
        {
            var users = Services.GetRequiredService<IUserService>();
            await using var _ = await WebHost.Serve();
            var webUsers = WebServices.GetRequiredService<IUserService>();

            async Task PingPong(IUserService users1, IUserService users2, User user)
            {
                var count0 = await users1.Count();
                (await users2.Count()).Should().Be(count0);

                await users1.Create(new(user));
                (await users1.Count()).Should().Be(++count0);

                await Delay(0.5);

                var user2 = await users2.TryGet(user.Id);
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
}
