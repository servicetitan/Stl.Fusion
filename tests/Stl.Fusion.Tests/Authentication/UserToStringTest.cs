using System.Collections.Immutable;
using Stl.Fusion.Authentication;
using Stl.Fusion.EntityFramework;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Authentication
{
    public class UserToStringTest : FusionTestBase
    {
        public UserToStringTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public void Test()
        {
            var user = new User("none") with {
                Claims = ImmutableDictionary<string, string>.Empty.Add("a", "b")
            };
            user = user.WithExternalId("Google", "G:1");
            Out.WriteLine(user.ToString());
        }
    }
}
