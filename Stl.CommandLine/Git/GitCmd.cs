using System;

namespace Stl.CommandLine.Git
{
    [Serializable]
    public class GitCmd : ShellLikeCmdBase
    {
        public static readonly CliString DefaultExecutable = CliString.New("git" + CmdHelpers.ExeExtension);

        public GitCmd(CliString? executable = null) : base(executable ?? DefaultExecutable) { }
    }
}
