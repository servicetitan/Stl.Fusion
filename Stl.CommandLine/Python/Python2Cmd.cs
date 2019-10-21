using System;

namespace Stl.CommandLine.Python
{
    [Serializable]
    public class Python2Cmd : ShellLikeCmdBase
    {
        public static readonly CliString DefaultExecutable = CliString.New("python2" + CmdHelpers.ExeExtension);

        public Python2Cmd(CliString? executable = null) : base(executable ?? DefaultExecutable) { }
    }
}