using Stl.Fusion.Operations.Internal;

namespace Stl.Fusion.Operations;

public static class OperationExt
{
    public static ClosedDisposable<(IOperation, ImmutableList<NestedCommandEntry>?)> SuppressNestedCommandLogging(
        this IOperation operation)
    {
        var nestedCommands = operation.Items.TryGet<ImmutableList<NestedCommandEntry>>();
        operation.Items.Remove<ImmutableList<NestedCommandEntry>>();
        return Disposable.NewClosed((operation, nestedCommands), state => {
            state.operation.Items.Set(state.nestedCommands);
        });
    }
}
