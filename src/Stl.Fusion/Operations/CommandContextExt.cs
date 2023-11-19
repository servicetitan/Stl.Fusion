using System.Diagnostics.CodeAnalysis;

namespace Stl.Fusion.Operations;

public static class CommandContextExt
{
    public static IOperation Operation(this CommandContext context)
        => context.Items.Get<IOperation>().Require();

    public static IOperation Operation<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicProperties)] TOperationScope>(this CommandContext context)
        where TOperationScope : class, IOperationScope
        => context.Items.Get<TOperationScope>().Require().Operation;

    public static void SetOperation(this CommandContext context, IOperation? operation)
        => context.Items.Set(operation);
}
