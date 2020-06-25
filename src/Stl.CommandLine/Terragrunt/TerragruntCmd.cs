using Stl.Collections;
using Stl.IO;

namespace Stl.CommandLine.Terragrunt
{
    public class TerragruntCmd : ProcessCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("terragrunt" + CmdHelpers.ExeExtension);

        public TerragruntCmd(PathString? executable = null) : base(executable ?? DefaultExecutable) { }

        public TerragruntCmd EnableLog(PathString logPath, string level = "TRACE")
        {
            EnvironmentVariables = EnvironmentVariables.SetItems(
                ("TF_LOG", level),
                ("TF_LOG_PATH", logPath));
            return this;
        }

        public TerragruntCmd DisableLog()
        {
            EnvironmentVariables = EnvironmentVariables
                .Remove("TF_LOG")
                .Remove("TF_LOG_PATH");
            return this;
        }
    }
}
