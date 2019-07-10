using Stl.CommandLine;
using Stl.OS;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandLine
{
    public class ShellTest : TestBase
    {
        public ShellTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async void EchoTest()
        {
            var shell = new Shell();
            Assert.Equal("hi", (await shell.GetOutputAsync("echo hi")).Trim());
            if (OSInfo.Kind == OSKind.Windows)
                Assert.Equal("^\"'", (await shell.GetOutputAsync("echo ^\"'")).Trim());
        }
    }
}
