using System.Security;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Services;
using Stl.Fusion.Tests.Model;
using Stl.Reflection;
using User = Stl.Fusion.Authentication.User;

namespace Stl.Fusion.Tests.Authentication;

public class SqliteAuthServiceTest : AuthServiceTestBase
{
    public SqliteAuthServiceTest(ITestOutputHelper @out) : base(@out)
        => DbType = FusionTestDbType.Sqlite;
}

public class PostgreSqlAuthServiceTest : AuthServiceTestBase
{
    public PostgreSqlAuthServiceTest(ITestOutputHelper @out) : base(@out)
    {
        DbType = FusionTestDbType.PostgreSql;
        UseRedisOperationLogChangeTracking = false;
    }
}

public class MariaDbAuthServiceTest : AuthServiceTestBase
{
    public MariaDbAuthServiceTest(ITestOutputHelper @out) : base(@out)
        => DbType = FusionTestDbType.MariaDb;
}

public class SqlServerAuthServiceTest : AuthServiceTestBase
{
    public SqlServerAuthServiceTest(ITestOutputHelper @out) : base(@out)
        => DbType = FusionTestDbType.SqlServer;
}

public class InMemoryAuthServiceTest : AuthServiceTestBase
{
    public InMemoryAuthServiceTest(ITestOutputHelper @out) : base(@out)
        => DbType = FusionTestDbType.InMemory;
}

public class InMemoryInMemoryAuthServiceTest : AuthServiceTestBase
{
    public InMemoryInMemoryAuthServiceTest(ITestOutputHelper @out) : base(@out)
    {
        DbType = FusionTestDbType.InMemory;
        UseInMemoryAuthService = true;
    }
}

public abstract class AuthServiceTestBase(ITestOutputHelper @out) : FusionTestBase(@out)
{
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

        var auth = Services.GetRequiredService<IAuth>();
        var tAuthService = typeof(DbAuthService<TestDbContext, DbAuthSessionInfo, DbAuthUser, long>);
        if (UseInMemoryAuthService)
            tAuthService = typeof(InMemoryAuthService);
        auth.GetType().NonProxyType().Should().Be(tAuthService);
    }

    [Fact]
    public async Task CreateUserTest()
    {
        if (MustSkip()) return;

        await using var serving = await WebHost.Serve();
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var authClient = ClientServices.GetRequiredService<IAuth>();

        for (var i = -100; i < 100; i++) {
            var user = await authBackend.GetUser(default, i.ToString());
            user.Should().BeNull();
        }

        var u1 = new User("Bob1").WithIdentity("g:1");
        var s1 = await CreateUser(u1);
        var u2 = new User("100500", "Bob2").WithIdentity("g:2");
        if (DbType == FusionTestDbType.SqlServer)
            u2 = u2 with { Id = "" }; // SQL Server doesn't support identity inserts by default
        var s2 = await CreateUser(u2);
        await Assert.ThrowsAsync<FormatException>(async () => {
            var s3 = await CreateUser(new User("invalid", "Bob3").WithIdentity("g:3"));
        });

        async Task<Session> CreateUser(User source)
        {
            var session = Session.New();
            await WebServices.Commander().Call(new AuthBackend_SignIn(session, source));

            var user = await authClient.GetUser(session);
            user.Should().NotBeNull();
            user!.Name.Should().Be(source.Name);
            long.TryParse(user.Id, out _).Should().BeTrue();
            user.Claims.Count.Should().Be(0);
            return session;
        }
    }

    [Fact]
    public async Task BasicTest1()
    {
        if (MustSkip()) return;

        await using var serving = await WebHost.Serve();
        var commander = Services.Commander();
        var auth = Services.GetRequiredService<IAuth>();
        var webCommander = WebServices.Commander();
        var webAuth = WebServices.GetRequiredService<IAuth>();
        var authClient = ClientServices.GetRequiredService<IAuth>();
        var sessionA = Session.New();
        var sessionB = Session.New();
        var bob = new User("Bob").WithIdentity("g:1");

        var session = sessionA;
        await webCommander.Call(new AuthBackend_SignIn(session, bob));
        var user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);
        long.TryParse(user.Id, out _).Should().BeTrue();
        user.Claims.Count.Should().Be(0);
        bob = user;

        // Trying to edit user name
        var newName = "Bobby";
        await webCommander.Call(new Auth_EditUser(session, newName));
        user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be(newName);
        bob = bob with { Name = newName };

        // Checking if the client is able to see the same user & sessions
        user = await authClient.GetUser(sessionA);
        user.Should().NotBeNull();
        user!.Id.Should().Be(bob.Id);
        user.ToClaimsPrincipal().Identity!.IsAuthenticated.Should().BeTrue();

        user = await authClient.GetUser(session);
        user.Should().NotBeNull();
        user!.Id.Should().Be(bob.Id);
        user.ToClaimsPrincipal().Identity!.IsAuthenticated.Should().BeTrue();

        // Checking if local service is able to see the same user & sessions
        if (!UseInMemoryAuthService) {
            await Delay(0.5);
            user = await auth.GetUser(session);
            user.Should().NotBeNull();
            user!.Id.Should().Be(bob.Id);
            user.ToClaimsPrincipal().Identity!.IsAuthenticated.Should().BeTrue();
        }

        // Checking guest session
        session = sessionB;
        user = await authClient.GetUser(session);
        user.Should().BeNull();

        // Checking sign-out
        await WebServices.Commander().Call(new Auth_SignOut(sessionA));
        user = await webAuth.GetUser(sessionA);
        user.Should().BeNull();

        await Delay(0.5);
        user = await authClient.GetUser(sessionA);
        user.Should().BeNull();
        if (!UseInMemoryAuthService) {
            user = await auth.GetUser(sessionA);
            user.Should().BeNull();
        }
    }

    [Fact]
    public async Task BasicTest2()
    {
        if (MustSkip()) return;

        await using var serving = await WebHost.Serve();
        var commander = Services.Commander();
        var auth = Services.GetRequiredService<IAuth>();
        var webCommander = WebServices.Commander();
        var webAuth = WebServices.GetRequiredService<IAuth>();
        var webAuthBackend = WebServices.GetRequiredService<IAuthBackend>();
        var authClient = ClientServices.GetRequiredService<IAuth>();
        var sessionA = Session.New();
        var sessionB = Session.New();
        var bob = new User("Bob")
            .WithClaim("id", "bob")
            .WithClaim("id2", "bob")
            .WithIdentity("g:1");

        var session = sessionA;
        await webCommander.Call(new AuthBackend_SignIn(session, bob));
        var user = await webAuth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);
        long.TryParse(user.Id, out _).Should().BeTrue();
        user.Claims.Count.Should().Be(2);
        user.Claims["id"].Should().Be("bob");
        _ = user.Identities.Single(); // Client-side users shouldn't have them

        bob = bob.WithClaim("id", "robert");
        await webCommander.Call(new AuthBackend_SignIn(session, bob));
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
        user.ToClaimsPrincipal().Identity!.IsAuthenticated.Should().BeTrue();
        user.Claims.Count.Should().Be(2);

        // Checking if local service is able to see the same user & sessions
        if (!UseInMemoryAuthService) {
            await Delay(0.5);
            user = await auth.GetUser(session);
            user.Should().NotBeNull();
            user!.Id.Should().Be(bob.Id);
            user.ToClaimsPrincipal().Identity!.IsAuthenticated.Should().BeTrue();
        }

        // Checking guest session
        session = sessionB;
        user = await authClient.GetUser(session);
        user.Should().BeNull();

        // Checking sign-out
        await webCommander.Call(new Auth_SignOut(sessionA));
        user = await webAuth.GetUser(sessionA);
        user.Should().BeNull();

        await Delay(0.5);
        user = await authClient.GetUser(sessionA);
        user.Should().BeNull();
        if (!UseInMemoryAuthService) {
            user = await auth.GetUser(sessionA);
            user.Should().BeNull();
        }
    }

    [Fact]
    public async Task GuestTest1()
    {
        if (MustSkip()) return;

        var auth = Services.GetRequiredService<IAuth>();

        var session = Session.New();
        var user = await auth.GetUser(session);
        user.Should().BeNull();

        user = user.OrGuest();
        user.Id.Value.Should().Be("");
        user.ToClaimsPrincipal().Identity!.IsAuthenticated.Should().BeFalse();
        user.Identities.Count.Should().Be(0);
    }

    [Fact]
    public async Task GuestTest2()
    {
        if (MustSkip()) return;

        var commander = Services.Commander();

        var session = Session.New();
        await Assert.ThrowsAsync<InvalidOperationException>(async() => {
            try {
                var guest = new User("notANumber", "Guest").WithIdentity("n:1");
                await commander.Call(new AuthBackend_SignIn(session, guest));
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

        var commander = Services.Commander();
        var auth = Services.GetRequiredService<IAuth>();

        var session = Session.New();
        var bob = new User("Bob").WithIdentity("b:1");
        await commander.Call(new AuthBackend_SignIn(session, bob));
        var user = await auth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be("Bob");

        await commander.Call(new Auth_EditUser(session, "John"));
        user = await auth.GetUser(session);
        user.Should().NotBeNull();
        user!.Name.Should().Be("John");
    }

    [Fact]
    public async Task LongFlowTest()
    {
        if (MustSkip()) return;

        var commander = Services.Commander();
        var auth = Services.GetRequiredService<IAuth>();
        var authBackend = Services.GetRequiredService<IAuthBackend>();
        var sessionA = Session.New();
        var sessionB = Session.New();

        var sessions = await auth.GetUserSessions(sessionA);
        sessions.Length.Should().Be(0);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        var bob = new User("Bob").WithIdentity("g:1");
        await commander.Call(new AuthBackend_SignIn(sessionA, bob));
        var user = await auth.GetUser(sessionA);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);
        bob = (await authBackend.GetUser("", user.Id)).Require(User.MustBeAuthenticated);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        await commander.Call(new AuthBackend_SignIn(sessionB, bob));
        user = await auth.GetUser(sessionB);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);

        await commander.Call(new Auth_SignOut(sessionA));
        (await auth.IsSignOutForced(sessionB)).Should().BeFalse();
        user = await auth.GetUser(sessionA);
        user.Should().BeNull();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Length.Should().Be(0);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionB.Hash);

        await commander.Call(new AuthBackend_SignIn(sessionA, bob));
        user = await auth.GetUser(sessionA);
        user.Should().NotBeNull();
        user!.Name.Should().Be(bob.Name);

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash, sessionB.Hash);

        await commander.Call(new Auth_SignOut(sessionB, true));
        (await auth.IsSignOutForced(sessionB)).Should().BeTrue();
        (await auth.GetAuthInfo(sessionB))!.IsSignOutForced.Should().BeTrue();
        user = await auth.GetUser(sessionB);
        user.Should().BeNull();

        sessions = await auth.GetUserSessions(sessionA);
        sessions.Select(s => s.SessionHash).Should().BeEquivalentTo(sessionA.Hash);
        sessions = await auth.GetUserSessions(sessionB);
        sessions.Length.Should().Be(0);

        await Assert.ThrowsAsync<SecurityException>(async() => {
            _ = await auth.GetAuthInfo(sessionB);
            await commander.Call(new AuthBackend_SetupSession(sessionB));
        });

        await Assert.ThrowsAsync<SecurityException>(async() => {
            await commander.Call(new AuthBackend_SignIn(sessionB, bob));
        });
    }
}
