using System.Threading;
using System.Threading.Tasks;
using CliWrap;
using Stl.Collections;
using Stl.IO;

namespace Stl.CommandLine.Terraform
{
    public class TerraformCmd : ProcessCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("terraform" + CmdHelpers.ExeExtension);

        public TerraformCmd(PathString? executable = null)
            : base(executable ?? DefaultExecutable)
        {
            EnvironmentVariables = EnvironmentVariables
                .SetItem("TF_VAR_is_bach_activated", "true");
        }

        public TerraformCmd EnableLog(PathString logPath, string level = "TRACE")
        {
            EnvironmentVariables = EnvironmentVariables.SetItems(
                ("TF_LOG", level),
                ("TF_LOG_PATH", logPath));
            return this;
        }

        public TerraformCmd DisableLog()
        {
            EnvironmentVariables = EnvironmentVariables
                .Remove("TF_LOG")
                .Remove("TF_LOG_PATH");
            return this;
        }

        public Task<CmdResult> ApplyAsync(
            CliString dir = default,
            ApplyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("apply", arguments ?? new ApplyArguments(), dir, cancellationToken);

        public Task<CmdResult> ImportAsync(
            CliString dir = default,
            ImportArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("import", arguments ?? new ImportArguments(), dir, cancellationToken);

        public Task<CmdResult> FmtAsync(
            CliString dir = default,
            FmtArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("fmt", arguments ?? new FmtArguments(), dir, cancellationToken);

        public async Task<CmdResult> InitAsync(
            CliString dir = default,
            InitArguments? arguments = null,
            CancellationToken cancellationToken = default)
        {
            // A workaround for this issue:
            // https://github.com/hashicorp/terraform/issues/21393 
            var oldEnv = EnvironmentVariables;
            EnvironmentVariables = oldEnv.SetItem("TF_WORKSPACE", "default");
            try {
                return await RunRawAsync("init", arguments ?? new InitArguments(), dir, cancellationToken)
                    .ConfigureAwait(false);
            }
            finally {
                EnvironmentVariables = oldEnv;
            }
        }

        public Task<CmdResult> DestroyAsync(
            CliString dir = default,
            DestroyArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("destroy", arguments ?? new DestroyArguments(), dir, cancellationToken);

        public Task<CmdResult> WorkspaceNewAsync(
            CliString workspaceName,
            CliString dirName = default,
            WorkspaceNewArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace new", arguments ?? new WorkspaceNewArguments(), workspaceName + dirName, cancellationToken);

        public Task<CmdResult> WorkspaceDeleteAsync(
            CliString workspaceName,
            CliString dirName = default,
            WorkspaceDeleteArguments? arguments = null,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace delete", arguments ?? new WorkspaceDeleteArguments(), workspaceName + dirName, cancellationToken);

        public Task<CmdResult> WorkspaceSelectAsync(
            CliString workspaceName,
            CliString dirName = default,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace select", null, workspaceName + dirName, cancellationToken);

        public Task<CmdResult> WorkspaceListAsync(
            CliString dirName = default,
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace list", null, dirName, cancellationToken);

        public Task<CmdResult> WorkspaceShowAsync(
            CancellationToken cancellationToken = default)
            => RunRawAsync("workspace show", null, default, cancellationToken);

        public async Task WorkspaceChangeAsync(string workspaceName, bool reset = false)
        {
            // This makes sure this method doesn't change ResultChecks 
            using var _ = this.ChangeResultValidation(0);
            CmdResult r;
            if (reset) {
                r = await WorkspaceDeleteAsync(
                    workspaceName, default, 
                    new WorkspaceDeleteArguments() {
                        Force = true,
                    }).ConfigureAwait(false);
            }
            r = await WorkspaceSelectAsync(workspaceName).ConfigureAwait(false);
            ResultValidation = CommandResultValidation.ZeroExitCode;
            if (r.ExitCode != 0) 
                r = await WorkspaceNewAsync(workspaceName).ConfigureAwait(false);
            r = await WorkspaceSelectAsync(workspaceName).ConfigureAwait(false);
        }
    }
}
