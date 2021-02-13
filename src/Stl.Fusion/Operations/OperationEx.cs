using System.Collections.Immutable;
using Stl.Fusion.Operations.Internal;

namespace Stl.Fusion.Operations
{
    public static class OperationEx
    {
        public static ClosedDisposable<(IOperation, ImmutableList<NestedCommand>?)> SuppressNestedCommandLogging(
            this IOperation operation)
        {
            var nestedCommands = operation.Items.TryGet<ImmutableList<NestedCommand>>();
            operation.Items.Remove<ImmutableList<NestedCommand>>();
            return Disposable.NewClosed((operation, nestedCommands), state => {
                state.operation.Items.Set(state.nestedCommands);
            });
        }
    }
}
