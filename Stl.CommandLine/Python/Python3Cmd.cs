using System;

namespace Stl.CommandLine.Python
{
    [Serializable]
    public class Python3Cmd : ShellLikeCmdBase
    {
        public static readonly CliString DefaultExecutable = CliString.New("python3" + CmdHelpers.ExeExtension);

        public Python3Cmd(CliString? executable = null) : base(executable ?? DefaultExecutable) { }
    }
}
