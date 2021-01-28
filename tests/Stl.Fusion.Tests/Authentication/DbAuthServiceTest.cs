using System;
using System.Linq;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.Extensions.DependencyInjection;
using Stl.CommandR;
using Stl.CommandR.Commands;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework;
using Stl.Fusion.Operations;
using Stl.Tests;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Authentication
{
    [Collection(nameof(TimeSensitiveTests)), Trait("Category", nameof(TimeSensitiveTests))]
    public class DbAuthServiceTest : FusionTestBase
    {
        public DbAuthServiceTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public async Task ContainerConfigTest()
        {
            await using var serving = await WebHost.ServeAsync();
            var agentInfo1 = WebServices.GetRequiredService<AgentInfo>();
            var agentInfo2 = Services.GetRequiredService<AgentInfo>();
            var notifier1 = WebServices.GetRequiredService<IOperationCompletionNotifier>();
            var notifier2 = Services.GetRequiredService<IOperationCompletionNotifier>();

            agentInfo1.Should().NotBe(agentInfo2);
            agentInfo1.Id.Should().NotBe(agentInfo2.Id);
            notifier1.Should().NotBe(notifier2);
        }

        [Fact]
        public async Task BasicTest1()
        {
            await using var serving = await WebHost.ServeAsync();
            var authServer = WebServices.GetRequiredService<IServerSideAuthService>();
            var authClient = ClientServices.GetRequiredService<IAuthService>();
            var authLocal = Services.GetRequiredService<IServerSideAuthService>();
            var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
            var sessionA = sessionFactory.CreateSession();
            var sessionB = sessionFactory.CreateSession();
            var bob = new User("", "Bob");

            var session = sessionA;
            await WebServices.Commander().CallAsync(new SignInCommand(bob, session).MarkServerSide());
            var user = await authServer.GetUserAsync(session);
            user.Name.Should().Be(bob.Name);
            long.TryParse(user.Id, out var _).Should().BeTrue();
            user.Claims.Count.Should().Be(0);
            bob = user;

            // Checking if the client is able to see the same user & sessions
            user = await authClient.GetUserAsync(sessionA);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();
            user = await authClient.GetUserAsync(session);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();

            // Checking if local service is able to see the same user & sessions
            await DelayAsync(0.5);
            user = await authLocal.GetUserAsync(session);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();

            // Checking guest session
            session = sessionB;
            user = await authClient.GetUserAsync(session);
            user.IsAuthenticated.Should().BeFalse();

            // Checking sign-out
            await WebServices.Commander().CallAsync(new SignOutCommand(false, sessionA));
            user = await authServer.GetUserAsync(sessionA);
            user.IsAuthenticated.Should().BeFalse();
            await DelayAsync(0.5);
            user = await authClient.GetUserAsync(sessionA);
            user.IsAuthenticated.Should().BeFalse();
            user = await authLocal.GetUserAsync(sessionA);
            user.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async Task BasicTest2()
        {
            await using var serving = await WebHost.ServeAsync();
            var authServer = WebServices.GetRequiredService<IServerSideAuthService>();
            var authClient = ClientServices.GetRequiredService<IAuthService>();
            var authLocal = Services.GetRequiredService<IServerSideAuthService>();
            var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
            var sessionA = sessionFactory.CreateSession();
            var sessionB = sessionFactory.CreateSession();
            var bob = new User("", "Bob")
                .WithClaim("id", "bob")
                .WithIdentity("g:1");

            var session = sessionA;
            await authServer.SignInAsync(new SignInCommand(bob, session).MarkServerSide());
            var user = await authServer.GetUserAsync(session);
            user.Name.Should().Be(bob.Name);
            long.TryParse(user.Id, out var _).Should().BeTrue();
            user.Claims.Count.Should().Be(1);
            user.Identities.Keys.Select(i => i.Id.Value).Should().BeEquivalentTo(new [] {"g:1"});
            bob = user;

            // Checking if the client is able to see the same user & sessions
            user = await authClient.GetUserAsync(sessionA);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();
            user = await authClient.GetUserAsync(session);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();

            // Checking if local service is able to see the same user & sessions
            await DelayAsync(0.5);
            user = await authLocal.GetUserAsync(session);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();

            // Checking guest session
            session = sessionB;
            user = await authClient.GetUserAsync(session);
            user.IsAuthenticated.Should().BeFalse();

            // Checking sign-out
            await authServer.SignOutAsync(new(false, sessionA));
            user = await authServer.GetUserAsync(sessionA);
            user.IsAuthenticated.Should().BeFalse();
            await DelayAsync(0.5);
            user = await authClient.GetUserAsync(sessionA);
            user.IsAuthenticated.Should().BeFalse();
            user = await authLocal.GetUserAsync(sessionA);
            user.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async Task GuestTest1()
        {
            var authServer = Services.GetRequiredService<IServerSideAuthService>();
            var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();

            var session = sessionFactory.CreateSession();
            var user = await authServer.GetUserAsync(session);
            user.Id.Should().Be(new User(session.Id).Id);
            user.Name.Should().Be(User.GuestName);
            user.IsAuthenticated.Should().BeFalse();
        }

        [Fact]
        public async Task GuestTest2()
        {
            var authServer = Services.GetRequiredService<IServerSideAuthService>();
            var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();

            var session = sessionFactory.CreateSession();
            await Assert.ThrowsAsync<FormatException>(async() => {
                var guest = new User("notANumber", "Guest");
                await authServer.SignInAsync(new SignInCommand(guest, session).MarkServerSide());
            });
            var bob = new User("", "Bob");
            await authServer.SignInAsync(new SignInCommand(bob, session).MarkServerSide());
            var user = await authServer.GetUserAsync(session);
            user.Name.Should().Be("Bob");
        }
    }
}
