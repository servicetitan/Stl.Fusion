using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stl.CommandLine;
using Stl.CommandLine.Terraform;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Terraform
{
    public class TerraformBasicTest : TestBase
    {
        protected TerraformCmd TerraformEcho { get; } = new TerraformCmd { EchoMode = true };

        public TerraformBasicTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task ApplyArgumentFormatTest()
        {
            var executionResult = await TerraformEcho.ApplyAsync("dir", 
                new ApplyArguments {
                    Backup = "Backup",
                    AutoApprove = false,
                    Variables = new CliDictionary<CliString, CliString>() {{"key1", "value1"}},
                    LockTimeout = 16,
                });
            executionResult.StandardOutput.Trim().Should()
                .Be(TerraformEcho  +
                    " apply " +
                    "-var \"key1=value1\" -lock-timeout=16s " +
                    "-backup=\"Backup\" " +
                    "dir");

            executionResult = await TerraformEcho.ApplyAsync(
                "dir", new ApplyArguments { NoColor = true });
            executionResult.StandardOutput.Trim().Should()
                .Be(TerraformEcho +
                    " apply " +
                    "-no-color " +
                    "-auto-approve " +
                    "dir");
        }

        [Fact]
        public async Task FormatterMockTest()
        {
            var formatter = new Mock<ICliFormatter>();
            var terraformEcho = new TerraformCmd {
                EchoMode = true,
                CliFormatter = formatter.Object
            };

            var applyArguments = new ApplyArguments();
            var formattedArguments = CliString.New("arguments");
            formatter
                .Setup(x => x.Format(It.IsAny<object>(), It.IsAny<CliArgumentAttribute>()))
                .Returns(formattedArguments);
            var dir = "dir";

            var result = (await terraformEcho.ApplyAsync(dir, arguments: applyArguments)).StandardOutput.Trim();

            result.Should().Be(terraformEcho + " apply arguments dir");
            formatter.Verify(x => x.Format(applyArguments, It.IsAny<CliArgumentAttribute>()));
        }
    }
}
