using System.Threading.Tasks;
using Stl.CommandLine;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandLine
{
    public class ShellTest : TestBase
    {
        public ShellTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task EchoTest()
        {
            var shell = new Shell() { };
            Assert.Equal("hi", (await shell.GetOutputAsync("echo hi")).Trim());
            
            var expected = "^\"'";
            var command = CliString.New("echo ") + CliString.Quote(expected);
            Assert.Equal(expected, (await shell.GetOutputAsync(command)).Trim());
        }
    }
}
