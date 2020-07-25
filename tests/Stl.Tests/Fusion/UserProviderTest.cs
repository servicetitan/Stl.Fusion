using System;
using System.Diagnostics;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Fusion;
using Stl.Tests.Fusion.Model;
using Stl.Tests.Fusion.Services;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Fusion
{
    public class UserProviderTest : FusionTestBase, IAsyncLifetime
    {
        public UserProviderTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task InvalidateEverythingTest()
        {
            var users = Container.Resolve<IUserService>();
            // We need at least 1 user to see count invalidation messages
            await users.CreateAsync(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }, true);

            var u1 = await users.TryGetAsync(int.MaxValue);
            var c1 = await Computed.CaptureAsync(_ => users.CountAsync());

            users.Invalidate();

            var u2 = await users.TryGetAsync(int.MaxValue);
            u2!.IsFrozen.Should().BeTrue();
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
            var users = Container.Resolve<IUserService>();
            // We need at least 1 user to see count invalidation messages
            await users.CreateAsync(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }, true);

            var userCount = await users.CountAsync();

            var u = new User() {
                Id = 1000,
                Name = "Bruce Lee"
            };
            // This delete won't do anything, since the user doesn't exist
            (await users.DeleteAsync(u)).Should().BeFalse();
            // Thus count shouldn't change
            (await users.CountAsync()).Should().Be(userCount);
            // But after this line the could should change
            await users.CreateAsync(u);

            var u1 = await users.TryGetAsync(u.Id);
            u1.Should().NotBeNull();
            u1.Should().NotBeSameAs(u); // Because it's fetched
            u1!.IsFrozen.Should().BeTrue();
            u1.Id.Should().Be(u.Id);
            u1.Name.Should().Be(u.Name);
            (await users.CountAsync()).Should().Be(++userCount);

            var u2 = await users.TryGetAsync(u.Id);
            u2.Should().BeSameAs(u1);

            u.Name = "Jackie Chan";
            await users.UpdateAsync(u); // u.Name change

            var u3 = await users.TryGetAsync(u.Id);
            u3.Should().NotBeNull();
            u3.Should().NotBeSameAs(u2);
            u3!.Id.Should().Be(u.Id);
            u3.Name.Should().Be(u.Name);
            (await users.CountAsync()).Should().Be(userCount);
        }

        [Fact]
        public async Task SimpleComputedTest()
        {
            var users = Container.Resolve<IUserService>();
            var time = Container.Resolve<ITimeService>();

            var u = new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            };
            await users.CreateAsync(u, true);

            var cText = await SimpleComputed.New<string>(
                async (prev, cancellationToken) => {
                    var norris = await users.TryGetAsync(int.MaxValue, cancellationToken).ConfigureAwait(false);
                    var now = await time.GetTimeAsync().ConfigureAwait(false);
                    return $"@ {now:hh:mm:ss.fff}: {norris?.Name ?? "(none)"}";
                }).UpdateAsync(false);

            using var _ = cText.AutoUpdate((cNext, rPrev, updateError) => {
                Log.LogInformation(cNext.Value);
            });

            for (var i = 1; i <= 10; i += 1) {
                u.Name = $"Chuck Norris Lvl{i}";
                await users.UpdateAsync(u);
                await Task.Delay(100);
            }

            var text = await cText.UseAsync();
            text.Should().EndWith("Lvl10");
        }

        [Fact]
        public async Task SuppressTest()
        {
            var time = Container.Resolve<ITimeService>();
            var count1 = 0;
            var count2 = 0;

#pragma warning disable 1998
            var c1 = await SimpleComputed.New<int>(async (prev, cancellationToken) => count1++)
                .UpdateAsync(false);
            var c2 = await SimpleComputed.New<int>(async (prev, cancellationToken) => count2++)
                .UpdateAsync(false);
#pragma warning restore 1998
            var c12 = await SimpleComputed.New<(int, int)>(
                async (prev, cancellationToken) => {
                    var a = await c1.UseAsync(cancellationToken);
                    using var _ = Computed.Suppress();
                    var b = await c2.UseAsync(cancellationToken);
                    return (a, b);
                }).UpdateAsync(false);

            var v12a = await c12.UseAsync();
            c1.Invalidate(); // Should increment c1 & impact c12
            var v12b = await c12.UseAsync();
            v12b.Should().Be((v12a.Item1 + 1, v12a.Item2));
            c2.Invalidate(); // Should increment c2, but shouldn't impact c12
            var v12c = await c12.UseAsync();
            v12c.Should().Be(v12b);
        }

        [Fact]
        public async Task KeepAliveTimeTest()
        {
            var users = Container.Resolve<IUserService>();

            var cUser0 = await Computed.CaptureAsync(_ => users.TryGetAsync(0));
            var cCount = await Computed.CaptureAsync(_ => users.CountAsync());

            cUser0!.Options.KeepAliveTime.Should().Be(Computed.DefaultKeepAliveTime);
            cCount!.Options.KeepAliveTime.Should().Be(TimeSpan.FromSeconds(5));
        }
    }
}
