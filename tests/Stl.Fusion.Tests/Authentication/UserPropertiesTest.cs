using System.Security.Claims;
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
        var cp = user.ToClaimsPrincipal();
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

        cp = user.ToClaimsPrincipal();
        cp.Claims.Count().Should().Be(4);
        ci = cp.Identities.Single();
        ci.IsAuthenticated.Should().BeTrue();
        cp.Identity.Should().BeSameAs(ci);

        user = User.NewGuest();
        user.Name.Should().Be(User.GuestName);
        user.IsGuest().Should().BeTrue();
        user.Claims.Count.Should().Be(0);
        user.Identities.Count.Should().Be(0);
        cp = user.ToClaimsPrincipal();
        var nameClaim = cp.Claims.Single();
        nameClaim.Type.Should().Be(ClaimTypes.Name);
        nameClaim.Value.Should().Be(user.Name);
        ci = cp.Identities.Single();
        ci.IsAuthenticated.Should().BeFalse();
        cp.Identity.Should().BeSameAs(ci);

        cp.Identity!.IsAuthenticated.Should().BeFalse();
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
