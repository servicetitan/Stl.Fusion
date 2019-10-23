using System;
using Stl.IO;

namespace Stl.CommandLine.Git
{
    [Serializable]
    public class GitCmd : ShellLikeCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("git" + CmdHelpers.ExeExtension);

        public GitCmd(PathString? executable = null) : base(executable ?? DefaultExecutable) { }
    }
}
