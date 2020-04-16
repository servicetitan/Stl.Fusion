using System.Threading.Tasks;
using Autofac;
using FluentAssertions;
using Stl.Purifier;
using Stl.Purifier.Autofac;
using Stl.Tests.Purifier.Model;
using Stl.Tests.Purifier.Services;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Purifier
{
    public class UserProviderTest : PurifierTestBase, IAsyncLifetime
    {
        public UserProviderTest(ITestOutputHelper @out) : base(@out) { }

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
            (await users.DeleteAsync(u)).Should().BeFalse();
            (await users.CountAsync()).Should().Be(userCount);

            await users.CreateAsync(u);
            var u1 = await users.TryGetAsync(u.Id);
            u1.Should().NotBeNull();
            u1.Should().NotBeSameAs(u);
            u1!.Id.Should().Be(u.Id);
            u1.Name.Should().Be(u.Name);
            (await users.CountAsync()).Should().Be(++userCount);
            
            var u2 = await users.TryGetAsync(u.Id);
            u2.Should().BeSameAs(u1);
            
            u.Name = "Jackie Chan";
            await users.UpdateAsync(u);
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

            var norris = new User() {
                Id = int.MaxValue,
                Name = "Chuck Norris",
            };
            await users.CreateAsync(norris, true);

            IComputed<string> c;
            using (var capture = ComputedCapture.New<string>()) {
                // ReSharper disable once HeapView.CanAvoidClosure
                await customFunction.Invoke(async ct => {
                    var norris = await users.TryGetAsync(int.MaxValue, ct).ConfigureAwait(false);
                    var norrisName = norris?.Name ?? "(none)";
                    var cNow = await time.GetTimeAsync(ct).ConfigureAwait(false);
                    return $"@ {cNow.Value}: {norrisName}";  
                }, default, CallOptions.Capture);
                c = capture.Captured!;
            }
            c.TrackChanges((cNext, _) => Out.WriteLine(cNext.Value));

            for (var i = 1; i <= 10; i += 1) {
                norris.Name = $"Chuck Norris Lvl{i}";
                await users.UpdateAsync(norris);
                await Task.Delay(500);
            }

            c = await c.RenewAsync();
            c.Value.Should().EndWith("Lvl10");
        }
    }
}
