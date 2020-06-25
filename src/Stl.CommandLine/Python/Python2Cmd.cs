using System;
using Stl.IO;

namespace Stl.CommandLine.Python
{
    [Serializable]
    public class Python2Cmd : ShellLikeCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("python2" + CmdHelpers.ExeExtension);

        public Python2Cmd(PathString? executable = null) : base(executable ?? DefaultExecutable) { }
    }
}
