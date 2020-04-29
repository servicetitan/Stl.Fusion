using System;
using Stl.IO;

namespace Stl.CommandLine.Python
{
    [Serializable]
    public class Python3Cmd : ShellLikeCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("python3" + CmdHelpers.ExeExtension);

        public Python3Cmd(PathString? executable = null) : base(executable ?? DefaultExecutable) { }
    }
}
