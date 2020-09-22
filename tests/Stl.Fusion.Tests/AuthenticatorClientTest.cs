using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
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
            var authServer = WebSocketHost.Services.GetRequiredService<IServerSideAuthService>();
            var authClient = Services.GetRequiredService<IAuthService>();
            var sessionA = new Session("a");
            var sessionB = new Session("b");
            var alice = new User("Local", "alice", "Alice");
            var bob   = new User("Local", "bob", "Bob");
            var guest = new User("<guest>");

            using (sessionA.Activate()) {
                await authServer.LoginAsync(bob);
                var user = await authServer.GetUserAsync();
                user.Name.Should().Be(bob.Name);

                user = await authClient.GetUserAsync(sessionA);
                user.Name.Should().Be(bob.Name);
                user = await authClient.GetUserAsync();
                user.Name.Should().Be(bob.Name);
            }

            using (sessionB.Activate()) {
                var user = await authClient.GetUserAsync();
                user.Id.Should().Be(sessionB.Id);
                user.Name.Should().Be(User.GuestName);
            }

            using (((Session?) null).Activate()) {
                var user = await authClient.GetUserAsync();
                // User.Id should be equal to new AuthSession.Id
                user.Id.Length.Should().BeGreaterThan(8);
                user.Name.Should().Be(User.GuestName);
            }
        }
    }
}
