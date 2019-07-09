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

        public CliString Executable { get; }
        public CliString ArgumentsPrefix { get; }

        public TerraformCmd(CliString? executable = null, CliString? argumentsPrefix = null)
        {
            Executable = executable ?? DefaultExecutable;
            ArgumentsPrefix = argumentsPrefix ?? CliString.New("");
        }

        public Task<ExecutionResult> ApplyAsync(CliString dir,
            ApplyArguments? arguments = null, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync("apply", arguments, dir, cancellationToken);

        public Task<ExecutionResult> FmtAsync(CliString dir,
            FmtArguments? arguments = null, 
            CancellationToken cancellationToken = default) 
            => RunRawAsync("fmt", arguments, dir, cancellationToken);

        protected override CliString GetExecutable() => Executable;
        protected override CliString GetArgumentsPrefix() => ArgumentsPrefix;
    }
}
