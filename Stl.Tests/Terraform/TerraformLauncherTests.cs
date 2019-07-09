using System.Threading;
using System.Threading.Tasks;
using Moq;
using Stl.CommandLine;
using Stl.Terraform;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Terraform
{
    // TODO: Fix this test.
    /*
    public class TerraformLauncherTests : TestBase
    {
        private readonly Mock<Shell> shell;
        private const string ToolPath = "TestPath";
        private readonly TerraformCmd _terraformCmd;
        
        private readonly Mock<IParameterSerializer> parameterSerializer;

        public TerraformLauncherTests(ITestOutputHelper @out) : base(@out)
        {
            shell = new Mock<Shell>();
            parameterSerializer = new Mock<IParameterSerializer>();

            _terraformCmd = new TerraformCmd(shell.Object, ToolPath, parameterSerializer.Object);
        }

        [Fact]
        public async Task Test()
        {
            var applyParameters = new ApplyArguments();
            var parameters = new CliString[]{"parameters"};
            parameterSerializer.Setup(x => x.Serialize(It.IsAny<IParameters>()))
                .Returns(parameters);
            var dirOrPlan = "dir";

            await _terraformCmd.ApplyAsync(applyParameters, dirOrPlan);
            
            parameterSerializer.Verify(x => x.Serialize(applyParameters));
            shell.Verify(x => x.RunAsync(
                It.Is<CliString>(x => x.ToString() == "TestPath parameters dir"), 
                CancellationToken.None));
        }
    }
    */
}
