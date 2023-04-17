namespace Stl.CommandR;

[StructLayout(LayoutKind.Auto)]
public readonly record struct CommandExecutionState(
    ImmutableArray<CommandHandler> Handlers,
    int NextHandlerIndex = 0)
{
    public bool IsFinal => NextHandlerIndex >= Handlers.Length;
    public CommandHandler NextHandler => Handlers[NextHandlerIndex];
    public CommandExecutionState NextState => new(Handlers, NextHandlerIndex + 1);

    public override string ToString()
        => $"{GetType().GetName()}({NextHandlerIndex}/{Handlers.Length})";

    public CommandHandler? FindFinalHandler()
        => FindFinalHandler(NextHandlerIndex);
    public CommandHandler? FindFinalHandler(int startIndex)
    {
        for (var i = startIndex; i < Handlers.Length; i++) {
            var handler = Handlers[i];
            if (!handler.IsFilter)
                return handler;
        }
        return null;
    }
}
