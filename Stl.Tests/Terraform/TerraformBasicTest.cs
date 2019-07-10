using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stl.CommandLine;
using Stl.Terraform;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Terraform
{
    public class TerraformBasicTest : TestBase
    {
        protected TerraformCmd TerraformEcho { get; } = new TerraformCmd(
            Shell.DefaultExecutable,
            Shell.DefaultPrefix + "echo");
        
        public TerraformBasicTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task CliFormatterTest()
        {
            var executionResult = await TerraformEcho.ApplyAsync("dir", 
                new ApplyArguments {
                    Backup = "Backup",
                    AutoApprove = true,
                    Variables = new CliDictionary<CliString, CliString>() {{"key1", "value1"}},
                    LockTimeout = 16,
                });
            executionResult.StandardOutput.Trim().Should()
                .Be("apply -backup=\"Backup\" -auto-approve " +
                    "-lock-timeout=16s -var \"key1=value1\" dir");
        }

        [Fact]
        public async Task FormatterMockTest()
        {
            var formatter = new Mock<ICliFormatter>();
            var terraformEcho = new TerraformCmd(
                Shell.DefaultExecutable, 
                Shell.DefaultPrefix + "echo", 
                formatter.Object);

            var applyArguments = new ApplyArguments();
            var formattedArguments = CliString.New("arguments");
            formatter
                .Setup(x => x.Format(It.IsAny<object>(), It.IsAny<CliArgumentAttribute>()))
                .Returns(formattedArguments);
            var dir = "dir";

            var result = (await terraformEcho.ApplyAsync(dir, applyArguments)).StandardOutput.Trim();

            result.Should().Be("apply arguments dir");
            formatter.Verify(x => x.Format(applyArguments, It.IsAny<CliArgumentAttribute>()));
        }
    }
}
