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

            (new SignInCommand(user, session)).PassThroughAllSerializers().User.Name.Should().Be(user.Name);
            (new SignOutCommand(true, session)).PassThroughAllSerializers().Session.Should().Be(session);
            (new EditUserCommand("X", session)).PassThroughAllSerializers().Session.Should().Be(session);
            (new SetupSessionCommand("a", "b", session)).PassThroughAllSerializers().Session.Should().Be(session);
        }
    }
}
