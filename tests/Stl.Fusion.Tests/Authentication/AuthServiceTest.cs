using System.Security;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Tests.Authentication;

public class SqliteAuthServiceTest : AuthServiceTestBase
{
    public SqliteAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions()) { }
}

// Internal = disabled, currently GitHub can't run such tests
internal class PostgreSqlAuthServiceTest : AuthServiceTestBase
{
    public PostgreSqlAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() { DbType = FusionTestDbType.PostgreSql }) { }
}

public class InMemoryAuthServiceTest : AuthServiceTestBase
{
    public InMemoryAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() { UseInMemoryAuthService = true } ) { }
}

public abstract class AuthServiceTestBase : FusionTestBase
{
    public AuthServiceTestBase(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options) { }

    [Fact]
    public async Task ContainerConfigTest()
    {
        await using var serving = await WebHost.Serve();
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
        await using var serving = await WebHost.Serve();
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var webAuth = WebServices.GetRequiredService<IAuth>();
        var webAuthBackend = WebServices.GetRequiredService<IAuthBackend>();
        var authClient = ClientServices.GetRequiredService<IAuth>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();
        var bob = new User("", "Bob").WithIdentity("g:1");

        var session = sessionA;
        await WebServices.Commander().Call(
            new SignInCommand(session, bob));
        var user = await webAuth.GetSessionUser(session);
        user.Name.Should().Be(bob.Name);
        long.TryParse(user.Id, out var _).Should().BeTrue();
        user.Claims.Count.Should().Be(0);
        bob = user;

        // Trying to edit user name
        var newName = "Bobby";
        await authClient.EditUser(new(session, newName));
        user = await webAuth.GetSessionUser(session);
        user.Name.Should().Be(newName);
        bob = bob with { Name = newName };

        // Checking if the client is able to see the same user & sessions
        user = await authClient.GetSessionUser(sessionA);
        user.Id.Should().Be(bob.Id);
        user.IsAuthenticated.Should().BeTrue();
        user = await authClient.GetSessionUser(session);
        user.Id.Should().Be(bob.Id);
        user.IsAuthenticated.Should().BeTrue();

        // Checking if local service is able to see the same user & sessions
        if (!Options.UseInMemoryAuthService) {
            await Delay(0.5);
            user = await auth.GetSessionUser(session);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();
        }

        // Checking guest session
        session = sessionB;
        user = await authClient.GetSessionUser(session);
        user.IsAuthenticated.Should().BeFalse();

        // Checking sign-out
        await WebServices.Commander().Call(new SignOutCommand(sessionA));
        user = await webAuth.GetSessionUser(sessionA);
        user.IsAuthenticated.Should().BeFalse();
        await Delay(0.5);
        user = await authClient.GetSessionUser(sessionA);
        user.IsAuthenticated.Should().BeFalse();
        if (!Options.UseInMemoryAuthService) {
            user = await auth.GetSessionUser(sessionA);
            user.IsAuthenticated.Should().BeFalse();
        }
    }

    [Fact]
    public async Task BasicTest2()
    {
        await using var serving = await WebHost.Serve();
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var webAuth = WebServices.GetRequiredService<IAuth>();
        var webAuthBackend = WebServices.GetRequiredService<IAuthBackend>();
        var authClient = ClientServices.GetRequiredService<IAuth>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();
        var bob = new User("", "Bob")
            .WithClaim("id", "bob")
            .WithClaim("id2", "bob")
            .WithIdentity("g:1");

        var session = sessionA;
        await webAuthBackend.SignIn(new SignInCommand(session, bob));
        var user = await webAuth.GetSessionUser(session);
        user.Name.Should().Be(bob.Name);
        long.TryParse(user.Id, out var _).Should().BeTrue();
        user.Claims.Count.Should().Be(2);
        user.Identities.Single(); // Client-side users shouldn't have them

        // Server-side methods to get the same user
        var sameUser = await webAuthBackend.GetUser(user.Id);
        sameUser!.Id.Should().Be(user.Id);
        sameUser.Name.Should().Be(user.Name);
        sameUser.Identities.Keys.Select(i => i.Id.Value).Should().BeEquivalentTo(new [] {"g:1"});
        bob = user;

        // Checking if the client is able to see the same user & sessions
        user = await authClient.GetSessionUser(session);
        user.Id.Should().Be(bob.Id);
        user.IsAuthenticated.Should().BeTrue();
        user.Claims.Count.Should().Be(2);

        // Checking if local service is able to see the same user & sessions
        if (!Options.UseInMemoryAuthService) {
            await Delay(0.5);
            user = await auth.GetSessionUser(session);
            user.Id.Should().Be(bob.Id);
            user.IsAuthenticated.Should().BeTrue();
        }

        // Checking guest session
        session = sessionB;
        user = await authClient.GetSessionUser(session);
        user.IsAuthenticated.Should().BeFalse();

        // Checking sign-out
        await webAuth.SignOut(new(sessionA));
        user = await webAuth.GetSessionUser(sessionA);
        user.IsAuthenticated.Should().BeFalse();
        await Delay(0.5);
        user = await authClient.GetSessionUser(sessionA);
        user.IsAuthenticated.Should().BeFalse();
        if (!Options.UseInMemoryAuthService) {
            user = await auth.GetSessionUser(sessionA);
            user.IsAuthenticated.Should().BeFalse();
        }
    }

    [Fact]
    public async Task GuestTest1()
    {
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();

        var session = sessionFactory.CreateSession();
        var user = await auth.GetSessionUser(session);
        user.Id.Should().Be(new User(session.Id).Id);
        user.Name.Should().Be(User.GuestName);
        user.IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public async Task GuestTest2()
    {
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();

        var session = sessionFactory.CreateSession();
        await Assert.ThrowsAsync<InvalidOperationException>(async() => {
            try {
                var guest = new User("notANumber", "Guest").WithIdentity("n:1");
                await authBackend.SignIn(new SignInCommand(session, guest));
            }
            catch (FormatException) {
                // Thrown by InMemoryAuthService
                throw new InvalidOperationException();
            }
        });
        var bob = new User("", "Bob").WithIdentity("b:1");
        await authBackend.SignIn(new SignInCommand(session, bob));
        var user = await auth.GetSessionUser(session);
        user.Name.Should().Be("Bob");
    }

    [Fact]
    public async Task LongFlowTest()
    {
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();

        var sessions = await auth.GetUserSessions(sessionA);
        sessions.Length.Should().Be(0);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        var bob = new User("", "Bob").WithIdentity("g:1");
        var signInCmd = new SignInCommand(sessionA, bob);
        await authBackend.SignIn(signInCmd);
        var user = await auth.GetSessionUser(sessionA);
        user.Name.Should().Be(bob.Name);
        bob = await authBackend.GetUser(user.Id)
            ?? throw new NullReferenceException();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionA.Id });
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        signInCmd = new SignInCommand(sessionB, bob);
        await authBackend.SignIn(signInCmd);
        user = await auth.GetSessionUser(sessionB);
        user.Name.Should().Be(bob.Name);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionA.Id, sessionB.Id });
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionA.Id, sessionB.Id });

        var signOutCmd = new SignOutCommand(sessionA);
        await auth.SignOut(signOutCmd);
        (await auth.IsSignOutForced(sessionB)).Should().BeFalse();
        user = await auth.GetSessionUser(sessionA);
        user.IsAuthenticated.Should().BeFalse();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Length.Should().Be(0);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionB.Id });

        signInCmd = new SignInCommand(sessionA, bob);
        await authBackend.SignIn(signInCmd);
        user = await auth.GetSessionUser(sessionA);
        user.Name.Should().Be(bob.Name);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionA.Id, sessionB.Id });
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionA.Id, sessionB.Id });

        signOutCmd = new SignOutCommand(sessionB, true);
        await auth.SignOut(signOutCmd);
        (await auth.IsSignOutForced(sessionB)).Should().BeTrue();
        (await auth.GetSessionInfo(sessionB)).IsSignOutForced.Should().BeTrue();
        user = await auth.GetSessionUser(sessionB);
        user.IsAuthenticated.Should().BeFalse();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.Id).Should().BeEquivalentTo(new[] { sessionA.Id });
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        await Assert.ThrowsAsync<SecurityException>(async() => {
            var sessionInfo = await auth.GetSessionInfo(sessionB);
            var setupSessionCmd = new SetupSessionCommand(sessionB);
            await authBackend.SetupSession(setupSessionCmd);
        });

        await Assert.ThrowsAsync<SecurityException>(async() => {
            signInCmd = new SignInCommand(sessionB, bob);
            await authBackend.SignIn(signInCmd);
        });
    }
}
