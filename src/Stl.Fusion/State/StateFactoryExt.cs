namespace Stl.Fusion;

public static class StateFactoryExt
{
    // NewMutable

    public static IMutableState<T> NewMutable<T>(
        this IStateFactory factory,
        T initialOutput = default!)
    {
        var options = new MutableState<T>.Options() {
            InitialOutput = initialOutput,
        };
        return factory.NewMutable(options);
    }

    // NewComputed

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        T initialOutput,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
    {
        var options = new ComputedState<T>.Options() {
            InitialOutput = initialOutput,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        IUpdateDelayer updateDelayer,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
    {
        var options = new ComputedState<T>.Options() {
            UpdateDelayer = updateDelayer,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        Result<T> initialOutput,
        IUpdateDelayer updateDelayer,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
    {
        var options = new ComputedState<T>.Options() {
            InitialOutput = initialOutput,
            UpdateDelayer = updateDelayer,
        };
        return factory.NewComputed(options, computer);
    }
}
