using System.Security;
using Stl.Fusion.Authentication;
using Stl.Requirements;

namespace Stl.Fusion.Tests;

public class RequireTest : TestBase
{
    public RequireTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public async Task UserTest()
    {
        var user = new User("1", "Bob");
        user.Require(User.MustBeAuthenticated);
        user.RequireResult(User.MustBeAuthenticated);
        user.Require(User.MustBeAuthenticated & FuncRequirement.New<User>(u => u?.Name == "Bob"));
        await Task.FromResult(user)!.Require(User.MustBeAuthenticated);
        await Task.FromResult(user)!.RequireResult(User.MustBeAuthenticated);
        await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated);
        await ValueTaskExt.FromResult(user)!.RequireResult(User.MustBeAuthenticated);

        user = User.NewGuest();
        Assert.ThrowsAny<SecurityException>(() => user.Require(User.MustBeAuthenticated));
        Assert.ThrowsAny<ResultException>(() => user.RequireResult(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(() => Task.FromResult(user)!.RequireResult(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(async () => await ValueTaskExt.FromResult(user)!.RequireResult(User.MustBeAuthenticated));

        user = null;
        Assert.ThrowsAny<SecurityException>(() => user.Require(User.MustBeAuthenticated));
        Assert.ThrowsAny<ResultException>(() => user.RequireResult(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(() => Task.FromResult(user)!.RequireResult(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(user)!.Require(User.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(async () => await ValueTaskExt.FromResult(user)!.RequireResult(User.MustBeAuthenticated));
    }

    [Fact]
    public async Task SessionAuthInfoTest()
    {
        var session = new Session("whatever-long-long-id");
        var authInfo = new SessionAuthInfo(session) { UserId = "1" };
        authInfo.Require(SessionAuthInfo.MustBeAuthenticated);
        authInfo.RequireResult(SessionAuthInfo.MustBeAuthenticated);
        await Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated);
        await Task.FromResult(authInfo)!.RequireResult(SessionAuthInfo.MustBeAuthenticated);
        await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated);
        await ValueTaskExt.FromResult(authInfo)!.RequireResult(SessionAuthInfo.MustBeAuthenticated);

        authInfo = new SessionInfo(session);
        Assert.ThrowsAny<SecurityException>(() => authInfo.Require(SessionAuthInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ResultException>(() => authInfo.RequireResult(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(() => Task.FromResult(authInfo)!.RequireResult(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(async () => await ValueTaskExt.FromResult(authInfo)!.RequireResult(SessionAuthInfo.MustBeAuthenticated));

        authInfo = null;
        Assert.ThrowsAny<SecurityException>(() => authInfo.Require(SessionAuthInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ResultException>(() => authInfo.RequireResult(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(() => Task.FromResult(authInfo)!.RequireResult(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(authInfo)!.Require(SessionAuthInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(async () => await ValueTaskExt.FromResult(authInfo)!.RequireResult(SessionAuthInfo.MustBeAuthenticated));
    }

    [Fact]
    public async Task SessionInfoTest()
    {
        var session = new Session("whatever-long-long-id");
        var sessionInfo = new SessionInfo(session) { UserId = "1" };
        sessionInfo.Require(SessionInfo.MustBeAuthenticated);
        sessionInfo.RequireResult(SessionInfo.MustBeAuthenticated);
        await Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated);
        await Task.FromResult(sessionInfo)!.RequireResult(SessionInfo.MustBeAuthenticated);
        await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated);
        await ValueTaskExt.FromResult(sessionInfo)!.RequireResult(SessionInfo.MustBeAuthenticated);

        sessionInfo = new SessionInfo(session);
        Assert.ThrowsAny<SecurityException>(() => sessionInfo.Require(SessionInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ResultException>(() => sessionInfo.RequireResult(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(() => Task.FromResult(sessionInfo)!.RequireResult(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.RequireResult(SessionInfo.MustBeAuthenticated));

        sessionInfo = null;
        Assert.ThrowsAny<SecurityException>(() => sessionInfo.Require(SessionInfo.MustBeAuthenticated));
        Assert.ThrowsAny<ResultException>(() => sessionInfo.RequireResult(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(() => Task.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(() => Task.FromResult(sessionInfo)!.RequireResult(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<SecurityException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.Require(SessionInfo.MustBeAuthenticated));
        await Assert.ThrowsAnyAsync<ResultException>(async () => await ValueTaskExt.FromResult(sessionInfo)!.RequireResult(SessionInfo.MustBeAuthenticated));
    }
}
