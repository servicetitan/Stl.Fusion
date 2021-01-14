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
            await users.CreateAsync(new(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }));

            var u1 = await users.TryGetAsync(int.MaxValue);
            var c1 = await Computed.CaptureAsync(_ => users.CountAsync());

            users.Invalidate();

            var u2 = await users.TryGetAsync(int.MaxValue);
            var c2 = await Computed.CaptureAsync(_ => users.CountAsync());

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
            await users.CreateAsync(new(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }));
            var userCount = await users.CountAsync();

            var u = new User() {
                Id = 1000,
                Name = "Bruce Lee"
            };
            // This delete won't do anything, since the user doesn't exist
            (await users.DeleteAsync(new(u))).Should().BeFalse();
            // Thus count shouldn't change
            (await users.CountAsync()).Should().Be(userCount);
            // But after this line the could should change
            await users.CreateAsync(new(u));

            var u1 = await users.TryGetAsync(u.Id);
            u1.Should().NotBeNull();
            u1.Should().NotBeSameAs(u); // Because it's fetched
            u1!.Id.Should().Be(u.Id);
            u1.Name.Should().Be(u.Name);
            (await users.CountAsync()).Should().Be(++userCount);

            var u2 = await users.TryGetAsync(u.Id);
            u2.Should().BeSameAs(u1);

            u = u with { Name = "Jackie Chan" };
            await users.UpdateAsync(new(u)); // u.Name change

            var u3 = await users.TryGetAsync(u.Id);
            u3.Should().NotBeNull();
            u3.Should().NotBeSameAs(u2);
            u3!.Id.Should().Be(u.Id);
            u3.Name.Should().Be(u.Name);
            (await users.CountAsync()).Should().Be(userCount);
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
            await users.CreateAsync(new(u));

            using var sText = await stateFactory.NewLive<string>(
                o => o.WithInstantUpdates(),
                async (s, cancellationToken) => {
                    var norris = await users.TryGetAsync(int.MaxValue, cancellationToken).ConfigureAwait(false);
                    var now = await time.GetTimeAsync().ConfigureAwait(false);
                    return $"@ {now:hh:mm:ss.fff}: {norris?.Name ?? "(none)"}";
                }).UpdateAsync(false);
            sText.Updated += (s, _) => Log.LogInformation($"{s.Value}");

            for (var i = 1; i <= 10; i += 1) {
                u = u with { Name = $"Chuck Norris Lvl{i}" };
                await users.CreateAsync(new(u, true));
                await Task.Delay(100);
            }

            var text = await sText.UseAsync();
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
            var s1 = await stateFactory.NewComputed<int>(async (s, ct) => count1++).UpdateAsync(false);
            var s2 = await stateFactory.NewComputed<int>(async (s, ct) => count2++).UpdateAsync(false);
#pragma warning restore 1998
            var s12 = await stateFactory.NewComputed<(int, int)>(
                async (s, cancellationToken) => {
                    var a = await s1.UseAsync(cancellationToken);
                    using var _ = Computed.IgnoreDependencies();
                    var b = await s2.UseAsync(cancellationToken);
                    return (a, b);
                }).UpdateAsync(false);

            var v12a = await s12.UseAsync();
            s1.Computed.Invalidate(); // Should increment c1 & impact c12
            var v12b = await s12.UseAsync();
            v12b.Should().Be((v12a.Item1 + 1, v12a.Item2));
            s2.Computed.Invalidate(); // Should increment c2, but shouldn't impact c12
            var v12c = await s12.UseAsync();
            v12c.Should().Be(v12b);
        }

        [Fact]
        public async Task KeepAliveTimeTest()
        {
            var users = Services.GetRequiredService<IUserService>();

            var cUser0 = await Computed.CaptureAsync(_ => users.TryGetAsync(0));
            var cCount = await Computed.CaptureAsync(_ => users.CountAsync());

            cUser0!.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(1));
            cCount!.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(1));
        }
    }
}
