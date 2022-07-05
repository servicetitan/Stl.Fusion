using System.Security;
using System.Security.Principal;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;

namespace Stl.Fusion.Tests.Authentication;

public class SqliteAuthServiceTest : AuthServiceTestBase
{
    public SqliteAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.Sqlite
        })
    { }
}

public class PostgreSqlAuthServiceTest : AuthServiceTestBase
{
    public PostgreSqlAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.PostgreSql
        })
    { }
}

public class MariaDbAuthServiceTest : AuthServiceTestBase
{
    public MariaDbAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.MariaDb
        })
    { }
}

public class SqlServerAuthServiceTest : AuthServiceTestBase
{
    public SqlServerAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.SqlServer
        })
    { }
}

public class InMemoryAuthServiceTest : AuthServiceTestBase
{
    public InMemoryAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.InMemory
        })
    { }
}

public class InMemoryInMemoryAuthServiceTest : AuthServiceTestBase
{
    public InMemoryInMemoryAuthServiceTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.InMemory,
            UseInMemoryAuthService = true } )
    { }
}

public abstract class AuthServiceTestBase : FusionTestBase
{
    protected AuthServiceTestBase(ITestOutputHelper @out, FusionTestOptions? options = null)
        : base(@out, options) { }

    [Fact]
    public async Task ContainerConfigTest()
    {
        if (MustSkip()) return;

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
        if (MustSkip()) return;

        await using var serving = await WebHost.Serve();
        var auth = Services.GetRequiredService<IAuth>();
        var webAuth = WebServices.GetRequiredService<IAuth>();
        var authClient = ClientServices.GetRequiredService<IAuth>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();
        var bob = new User("Bob").WithIdentity("g:1");

        var session = sessionA;
        await WebServices.Commander().Call(
            new SignInCommand(session, bob));
        var user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);
        long.TryParse(user.Id, out _).Should().BeTrue();
        user.Claims.Count.Should().Be(0);
        bob = user;

        // Trying to edit user name
        var newName = "Bobby";
        await authClient.EditUser(new(session, newName));
        user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be(newName);
        bob = bob with { Name = newName };

        // Checking if the client is able to see the same user & sessions
        user = await authClient.GetUser(sessionA);
        user.Should().NotBeNull();
        user!.Id.Should().Be(bob.Id);
        (user as IIdentity).IsAuthenticated.Should().BeTrue();

        user = await authClient.GetUser(session);
        user.Should().NotBeNull();
        user!.Id.Should().Be(bob.Id);
        (user as IIdentity).IsAuthenticated.Should().BeTrue();

        // Checking if local service is able to see the same user & sessions
        if (!Options.UseInMemoryAuthService) {
            await Delay(0.5);
            user = await auth.GetUser(session);
            user.Should().NotBeNull();
            user!.Id.Should().Be(bob.Id);
            (user as IIdentity).IsAuthenticated.Should().BeTrue();
        }

        // Checking guest session
        session = sessionB;
        user = await authClient.GetUser(session);
        user.Should().BeNull();

        // Checking sign-out
        await WebServices.Commander().Call(new SignOutCommand(sessionA));
        user = await webAuth.GetUser(sessionA);
        user.Should().BeNull();

        await Delay(0.5);
        user = await authClient.GetUser(sessionA);
        user.Should().BeNull();
        if (!Options.UseInMemoryAuthService) {
            user = await auth.GetUser(sessionA);
            user.Should().BeNull();
        }
    }

    [Fact]
    public async Task BasicTest2()
    {
        if (MustSkip()) return;

        await using var serving = await WebHost.Serve();
        var auth = Services.GetRequiredService<IAuth>();
        var webAuth = WebServices.GetRequiredService<IAuth>();
        var webAuthBackend = WebServices.GetRequiredService<IAuthBackend>();
        var authClient = ClientServices.GetRequiredService<IAuth>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();
        var bob = new User("Bob")
            .WithClaim("id", "bob")
            .WithClaim("id2", "bob")
            .WithIdentity("g:1");

        var session = sessionA;
        await webAuthBackend.SignIn(new SignInCommand(session, bob));
        var user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);
        long.TryParse(user.Id, out _).Should().BeTrue();
        user.Claims.Count.Should().Be(2);
        user.Claims["id"].Should().Be("bob");
        _ = user.Identities.Single(); // Client-side users shouldn't have them

        bob = bob.WithClaim("id", "robert");
        await webAuthBackend.SignIn(new SignInCommand(session, bob));
        user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Claims.Count.Should().Be(2);
        user.Claims["id"].Should().Be("robert");

        // Server-side methods to get the same user
        var sameUser = await webAuthBackend.GetUser(default, user.Id);
        sameUser!.Id.Should().Be(user.Id);
        sameUser.Name.Should().Be(user.Name);
        sameUser.Identities.Keys.Select(i => i.Id.Value).Should().BeEquivalentTo("g:1");
        bob = user;

        // Checking if the client is able to see the same user & sessions
        user = await authClient.GetUser(session);
        user.Should().NotBeNull();
        user!.Id.Should().Be(bob.Id);
        (user as IIdentity).IsAuthenticated.Should().BeTrue();
        user.Claims.Count.Should().Be(2);

        // Checking if local service is able to see the same user & sessions
        if (!Options.UseInMemoryAuthService) {
            await Delay(0.5);
            user = await auth.GetUser(session);
            user.Should().NotBeNull();
            user!.Id.Should().Be(bob.Id);
            (user as IIdentity).IsAuthenticated.Should().BeTrue();
        }

        // Checking guest session
        session = sessionB;
        user = await authClient.GetUser(session);
        user.Should().BeNull();

        // Checking sign-out
        await webAuth.SignOut(new(sessionA));
        user = await webAuth.GetUser(sessionA);
        user.Should().BeNull();

        await Delay(0.5);
        user = await authClient.GetUser(sessionA);
        user.Should().BeNull();
        if (!Options.UseInMemoryAuthService) {
            user = await auth.GetUser(sessionA);
            user.Should().BeNull();
        }
    }

    [Fact]
    public async Task GuestTest1()
    {
        if (MustSkip()) return;

        var auth = Services.GetRequiredService<IAuth>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();

        var session = sessionFactory.CreateSession();
        var user = await auth.GetUser(session);
        user.Should().BeNull();

        user = user.OrGuest(session);
        user.Id.Value.Should().Be("");
        (user as IIdentity).IsAuthenticated.Should().BeFalse();
        user.Identities.Count.Should().Be(1);
        user.Identities.Contains(KeyValuePair.Create(UserIdentity.SessionHashIdentity, session.Hash)).Should().BeTrue();
    }

    [Fact]
    public async Task GuestTest2()
    {
        if (MustSkip()) return;

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
    }

    [Fact]
    public async Task EditTest()
    {
        if (MustSkip()) return;

        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();

        var session = sessionFactory.CreateSession();
        var bob = new User("Bob").WithIdentity("b:1");
        await authBackend.SignIn(new SignInCommand(session, bob));
        var user = await auth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be("Bob");

        await auth.EditUser(new(session, "John"));
        user = await auth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be("John");

#if NET5_0_OR_GREATER
        if (Options.DbType != FusionTestDbType.InMemory
            && Options.DbType != FusionTestDbType.MariaDb) {
            await Assert.ThrowsAnyAsync<Exception>(async () => {
                await auth.EditUser(new(session, "Jo"));
            });
        }
#endif
        user = await auth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be("John");
    }

    [Fact]
    public async Task LongFlowTest()
    {
        if (MustSkip()) return;

        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var sessionFactory = ClientServices.GetRequiredService<ISessionFactory>();
        var sessionA = sessionFactory.CreateSession();
        var sessionB = sessionFactory.CreateSession();

        var sessions = await auth.GetUserSessions(sessionA);
        sessions.Length.Should().Be(0);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        var bob = new User("Bob").WithIdentity("g:1");
        var signInCmd = new SignInCommand(sessionA, bob);
        await authBackend.SignIn(signInCmd);
        var user = await auth.GetUser(sessionA);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);
        bob = (await authBackend.GetUser("", user.Id)).AssertNotNull();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        signInCmd = new SignInCommand(sessionB, bob);
        await authBackend.SignIn(signInCmd);
        user = await auth.GetUser(sessionB);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);

        var signOutCmd = new SignOutCommand(sessionA);
        await auth.SignOut(signOutCmd);
        (await auth.IsSignOutForced(sessionB)).Should().BeFalse();
        user = await auth.GetUser(sessionA);
        user.Should().BeNull();
        (user.OrGuest() as IIdentity).IsAuthenticated.Should().BeFalse();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Length.Should().Be(0);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionB.Hash);

        signInCmd = new SignInCommand(sessionA, bob);
        await authBackend.SignIn(signInCmd);
        user = await auth.GetUser(sessionA);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);

        signOutCmd = new SignOutCommand(sessionB, true);
        await auth.SignOut(signOutCmd);
        (await auth.IsSignOutForced(sessionB)).Should().BeTrue();
        (await auth.GetAuthInfo(sessionB)).IsSignOutForced.Should().BeTrue();
        user = await auth.GetUser(sessionB);
        user.Should().BeNull();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        await Assert.ThrowsAsync<SecurityException>(async() => {
            _ = await auth.GetAuthInfo(sessionB);
            var setupSessionCmd = new SetupSessionCommand(sessionB);
            await authBackend.SetupSession(setupSessionCmd);
        });

        await Assert.ThrowsAsync<SecurityException>(async() => {
            signInCmd = new SignInCommand(sessionB, bob);
            await authBackend.SignIn(signInCmd);
        });
    }
}
