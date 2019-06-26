namespace Stl.Cmd
{
    public static class CmdEx
    {
        public static RawCmdBuilder<RawCmdResult> WithHelpOption<TCmd>(
            this TCmd cmd, string helpOption = "--help")
            where TCmd : CmdBuilder
            => Cmd.Raw(cmd.GetExecutable(), helpOption);
    }
}
