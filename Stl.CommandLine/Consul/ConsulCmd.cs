using Stl.IO;

namespace Stl.CommandLine.Consul
{
    public class ConsulCmd : ProcessCmdBase
    {
        public static readonly PathString DefaultExecutable = CliString.New("consul" + CmdHelpers.ExeExtension);

        public ConsulCmd(PathString? executable = null)
            : base(executable ?? DefaultExecutable)
        { }
    }
}
