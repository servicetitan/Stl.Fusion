using System.Threading;
using System.Threading.Tasks;
using Moq;
using Stl.OS;
using Stl.ParametersSerializer;
using Stl.Terraform;
using Stl.Terraform.Parameters;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.Terraform
{
    public class TerraformLauncherTests : TestBase
    {
        private readonly Mock<Shell> shell;
        private const string ToolPath = "TestPath";
        private readonly TerraformLauncher terraformLauncher;
        
        private readonly Mock<IParameterSerializer> parameterSerializer;

        public TerraformLauncherTests(ITestOutputHelper @out) : base(@out)
        {
            shell = new Mock<Shell>();
            parameterSerializer = new Mock<IParameterSerializer>();

            terraformLauncher = new TerraformLauncher(shell.Object, ToolPath, parameterSerializer.Object);
        }

        [Fact]
        public async Task Test()
        {
            var applyParameters = new ApplyParameters();
            var parameters = "parameters";
            parameterSerializer.Setup(x => x.Serialize(It.IsAny<IParameters>()))
                .Returns(new[] {parameters});
            
            await terraformLauncher.ApplyAsync(applyParameters);
            
            parameterSerializer.Verify(x => x.Serialize(applyParameters));
            shell.Verify(x => x.RunAsync(
                It.Is<CmdPart>(x => x.ToString() == ToolPath + " " + parameters), 
                CancellationToken.None));
        }
    }
}