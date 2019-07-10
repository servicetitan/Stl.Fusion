using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using Stl.CommandLine;

namespace Stl.Terraform
{
    public class TerraformCmd : Cmd
    {
        public static readonly CliString DefaultExecutable = 
            CliString.New("terraform").VaryByOS("terraform.exe");

        public TerraformCmd(
            CliString? executable = null, 
            CliString? argumentsPrefix = null, 
            ICliFormatter cliFormatter = null) 
            : base(
                executable ?? DefaultExecutable, 
                argumentsPrefix ?? "", 
                cliFormatter) { }

        public Task<ExecutionResult> ApplyAsync(CliString dir,
            ApplyArguments? arguments = null, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync("apply", arguments, dir, cancellationToken);

        public Task<ExecutionResult> FmtAsync(CliString dir,
            FmtArguments? arguments = null, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync("fmt", arguments, dir, cancellationToken);
    }
}
