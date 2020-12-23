using System.Collections.Immutable;
using Stl.Fusion.Authentication;
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
            var user = new User("") with {
                Claims = ImmutableDictionary<string, string>.Empty.Add("a", "b")
            };
            Out.WriteLine(user.ToString());
        }
    }
}
