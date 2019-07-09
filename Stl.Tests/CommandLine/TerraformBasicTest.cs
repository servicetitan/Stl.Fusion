using System.Collections.Generic;
using System.Threading.Tasks;
using FluentAssertions;
using Stl.CommandLine;
using Stl.Terraform;
using Stl.Testing;
using Xunit;
using Xunit.Abstractions;

namespace Stl.Tests.CommandLine
{
    public class TerraformBasicTest : TestBase
    {
        protected TerraformCmd Terraform { get; } = new TerraformCmd(
            CliString.New("echo").VaryByOS("cmd.exe"),
            CliString.Empty.VaryByOS("/C echo"));
        
        public TerraformBasicTest(ITestOutputHelper @out) : base(@out) { }

        [Fact]
        public async Task BasicTest()
        {
            var executionResult = await Terraform.ApplyAsync("dir", 
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
    }
}
