using System.Threading;
using System.Threading.Tasks;
using CliWrap.Models;
using Stl.IO;

namespace Stl.CommandLine.Terraform
{
    public class TerraformCmd : CmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("terraform" + CmdHelpers.ExeExtension);

        public TerraformCmd(PathString? executable = null)
            : base(executable ?? DefaultExecutable)
        { }

        public Task<ExecutionResult> ApplyAsync(
            CliString dir = default,
            ApplyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("apply", arguments ?? new ApplyArguments(), dir, cancellationToken);

        public Task<ExecutionResult> ImportAsync(
            CliString dir = default,
            ImportArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("import", arguments ?? new ImportArguments(), dir, cancellationToken);

        public Task<ExecutionResult> FmtAsync(
            CliString dir = default,
            FmtArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("fmt", arguments ?? new FmtArguments(), dir, cancellationToken);

        public Task<ExecutionResult> InitAsync(
            CliString dir = default,
            InitArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("init", arguments ?? new InitArguments(), dir, cancellationToken);

        public Task<ExecutionResult> DestroyAsync(
            CliString dir = default,
            DestroyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("destroy", arguments ?? new DestroyArguments(), dir, cancellationToken);

        public Task<ExecutionResult> WorkspaceNewAsync(
            CliString workspaceName,
            CliString dirName = default,
            WorkspaceNewArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace new", arguments ?? new WorkspaceNewArguments(), workspaceName + dirName, cancellationToken);

        public Task<ExecutionResult> DeleteWorkspaceAsync(
            CliString workspaceName,
            CliString dirName = default,
            WorkspaceDeleteArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace delete", arguments ?? new WorkspaceDeleteArguments(), workspaceName + dirName, cancellationToken);

        public Task<ExecutionResult> WorkspaceSelectAsync(
            CliString workspaceName,
            CliString dirName = default,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace select", null, workspaceName + dirName, cancellationToken);

        public Task<ExecutionResult> WorkspaceListAsync(
            CliString dirName = default,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace list", null, dirName, cancellationToken);

        public Task<ExecutionResult> WorkspaceShowAsync(
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace show", null, default, cancellationToken);
    }
}
