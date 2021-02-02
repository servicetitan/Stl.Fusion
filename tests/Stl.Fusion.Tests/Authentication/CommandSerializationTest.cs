using FluentAssertions;
using Stl.Fusion.Authentication;
using Stl.Fusion.Authentication.Commands;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Fusion.Tests.Authentication
{
    public class CommandSerializationTest : FusionTestBase
    {
        public CommandSerializationTest(ITestOutputHelper @out, FusionTestOptions? options = null) : base(@out, options) { }

        [Fact]
        public void AuthCommandTest()
        {
            var user = new User("b", "bob").WithIdentity("g:1");
            var session = new Session("validSessionId");

            new SignInCommand(session, user).PassThroughAllSerializers().User.Name.Should().Be(user.Name);
            new SignOutCommand(session, true).PassThroughAllSerializers().Session.Should().Be(session);
            new EditUserCommand(session, "X").PassThroughAllSerializers().Session.Should().Be(session);
            new SetupSessionCommand(session, "a", "b").PassThroughAllSerializers().Session.Should().Be(session);
        }
    }
}
