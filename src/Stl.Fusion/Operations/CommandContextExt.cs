namespace Stl.Fusion.Operations;

public static class CommandContextExt
{
    public static IOperation Operation(this CommandContext context)
        => context.Items.Get<IOperation>() ?? throw new KeyNotFoundException();

    public static IOperation Operation<TOperationScope>(this CommandContext context)
        where TOperationScope : class, IOperationScope
        => (context.Items.Get<TOperationScope>() ?? throw new KeyNotFoundException()).Operation;

    public static void SetOperation(this CommandContext context, IOperation? operation)
        => context.Items.Set(operation);
}
