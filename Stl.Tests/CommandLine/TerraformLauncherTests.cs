using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Moq;
using Stl.CommandLine;
using Stl.Terraform;
using Stl.Terraform.Parameters;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandLine
{
    public class TerraformLauncherTests : TestBase
    {
        private const string ToolPath = "echo";
        private readonly TerraformLauncher terraformLauncher;
        
        public TerraformLauncherTests(ITestOutputHelper @out) : base(@out)
        {
            terraformLauncher = new TerraformLauncher(toolPath: "echo");
        }

        [Fact]
        public async Task Test()
        {
            var executionResult = await terraformLauncher.ApplyAsync(new ApplyParameters
            {
                Backup = "Backup",
                AutoApprove = true,
                Variable = new Dictionary<string, string>{{"key1", "value1"}},
                LockTimeout = 16,
            }, "dir");
            executionResult.StandardOutput.Trim().Should()
                .Be("-backup=Backup -lock-timeout=16s -auto-approve -var key1=value1 dir");
        }
    }
}
