using System;
using System.Threading;
using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Microsoft.Extensions.Logging;
using Stl.Purifier;
using Stl.Purifier.Autofac;
using Stl.Purifier.Internal;
using Stl.Tests.Purifier.Model;
using Stl.Tests.Purifier.Services;
using Stl.Time;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public class UserProviderTest : PurifierTestBase, IAsyncLifetime
    {
        public UserProviderTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task InvalidateEverythingTest()
        {
            var users = Container.Resolve<IUserProvider>();
            // We need at least 1 user to see count invalidation messages
            await users.CreateAsync(new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            }, true);

            var u1 = await users.TryGetAsync(int.MaxValue);
            var c1 = await Computed.CaptureAsync(() => users.CountAsync());
            
            users.Invalidate();

            var u2 = await users.TryGetAsync(int.MaxValue);
            var c2 = await Computed.CaptureAsync(() => users.CountAsync());
            
            u2.Should().NotBeSameAs(u1);
            u2!.Id.Should().Be(u1!.Id);
            u2.Name.Should().Be(u1.Name);

            c2.Should().NotBeSameAs(c1);
            c2!.Value.Should().Be(c1!.Value); 
        }

        [Fact]
        public async Task InvalidationTest()
        {
            var users = Container.Resolve<IUserProvider>();
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
            u1!.Id.Should().Be(u.Id);
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
        public async Task CustomFunctionTest()
        {
            var users = Container.Resolve<IUserProvider>();
            var time = Container.Resolve<ITimeProvider>();
            var customFunction = Container.Resolve<CustomFunction>();

            var u = new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            };
            await users.CreateAsync(u, true);

            var cText = await Computed.CaptureAsync(
                () => customFunction.InvokeAsync(async ct => {
                    var norris = await users.TryGetAsync(int.MaxValue, ct).ConfigureAwait(false);
                    var now = await time.GetTimeAsync().ConfigureAwait(false);
                    return $"@ {now:hh:mm:ss.fff}: {norris?.Name ?? "(none)"}";  
                }, CancellationToken.None));
            
            using var _ = cText!.AutoRenew(
                (cNext, rPrev, invalidatedBy) => Log.LogInformation(cNext.Value));

            for (var i = 1; i <= 10; i += 1) {
                u.Name = $"Chuck Norris Lvl{i}";
                await users.UpdateAsync(u);
                await Task.Delay(100);
            }

            cText = (await cText!.RenewAsync())!;
            cText!.Value.Should().EndWith("Lvl10");
        }

        [Fact]
        public async Task KeepAliveTimeTest()
        {
            var users = Container.Resolve<IUserProvider>();

            var cUser0 = await Computed.CaptureAsync(() => users.TryGetAsync(0));
            var cCount = await Computed.CaptureAsync(() => users.CountAsync());

            cUser0!.KeepAliveTime.Should().Be(Computed.DefaultKeepAliveTime);
            cCount!.KeepAliveTime.Should().Be(IntMoment.SecondsToUnits(5));
        }
    }
}
