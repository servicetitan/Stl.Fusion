using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Client.Authentication;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class AuthenticatorClientTest : FusionTestBase
    {
        public AuthenticatorClientTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public async Task BasicTest()
        {
            await using var serving = await WebSocketHost.ServeAsync();
            var auth = Services.GetRequiredService<IServerAuthService>();
            var authClient = Services.GetRequiredService<IAuthClient>();
            var sessionAccessor = Services.GetRequiredService<IAuthSessionAccessor>();
            var sessionA = new AuthSession("a");
            var sessionB = new AuthSession("b");
            var alice = new AuthUser("alice", "Alice", "Local");
            var bob   = new AuthUser("bob", "Bob", "Local");

            sessionAccessor.Session = sessionA;
            await auth.LoginAsync(bob);
            var user = await auth.GetUserAsync();
            user.Name.Should().Be(bob.Name);

            user = await authClient.GetUserAsync(sessionA);
            user.Name.Should().Be(bob.Name);
            user = await authClient.GetUserAsync();
            user.Name.Should().Be(bob.Name);

            sessionAccessor.Session = sessionB;
            user = await authClient.GetUserAsync();
            user.Id.Should().Be(sessionB.Id);
            user.Name.Should().Be(AuthUser.GuestName);

            sessionAccessor.Session = null;
            user = await authClient.GetUserAsync();
            // User.Id should be equal to new AuthSession.Id
            user.Id.Length.Should().BeGreaterThan(8);
            user.Name.Should().Be(AuthUser.GuestName);
        }
    }
}
