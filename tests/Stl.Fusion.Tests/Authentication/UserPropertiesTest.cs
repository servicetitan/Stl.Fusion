using System.Security.Principal;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Tests.Authentication;

public class UserPropertiesTest : FusionTestBase
{
    public UserPropertiesTest(ITestOutputHelper @out)
        : base(@out, new FusionTestOptions() {
            DbType = FusionTestDbType.InMemory
        })
    { }

    [Fact]
    public void BasicTest()
    {
        var user = new User("none", "none");
        var cp = user.ClaimsPrincipal;
        cp.Claims.Count().Should().Be(3);
        var ci = cp.Identities.Single();
        ci.IsAuthenticated.Should().BeTrue();

        user = user
            .WithClaim("a", "b")
            .WithIdentity("Google/1", "Secret");
        user.Claims.Should().BeEquivalentTo(new [] {
            KeyValuePair.Create("a", "b")
        });
        var uid = user.Identities.Keys.Single();
        var (authType, userId) = uid;
        authType.Should().Be("Google");
        userId.Should().Be("1");
        user.Identities[uid].Should().Be("Secret");

        cp = user.ClaimsPrincipal;
        cp.Claims.Count().Should().Be(4);
        ci = cp.Identities.Single();
        ci.IsAuthenticated.Should().BeTrue();

        user = User.NewGuest();
        user.Name.Should().Be(User.GuestName);
        user.Id.IsEmpty.Should().BeTrue();
        user.Identities.Count.Should().Be(0);
        (user as IIdentity).IsAuthenticated.Should().BeFalse();

        user = User.NewGuest(Session.Default);
        user.Name.Should().Be(User.GuestName);
        user.Id.IsEmpty.Should().BeTrue();
        user.Identities.Count.Should().Be(1);
        user.Identities.Contains(KeyValuePair.Create(UserIdentity.SessionHashIdentity, Session.Default.Hash)).Should().BeTrue();
        (user as IIdentity).IsAuthenticated.Should().BeFalse();
    }

    [Fact]
    public void ParseTest()
    {
        (string AuthType, string UserId) Parse(UserIdentity userIdentity) {
            var (authType, userId) = userIdentity;
            return (authType, userId);
        }

        Parse("1").Should().Be((UserIdentity.DefaultSchema, "1"));
        Parse("1/2").Should().Be(("1", "2"));
        Parse("1\\/2").Should().Be((UserIdentity.DefaultSchema, "1/2"));
    }
}
