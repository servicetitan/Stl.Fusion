namespace Stl.Fusion;

public static class StateFactoryExt
{
    // NewMutable

    public static IMutableState<T> NewMutable<T>(
        this IStateFactory factory,
        T initialValue = default!)
    {
        var options = new MutableState<T>.Options() {
            InitialValue = initialValue,
        };
        return factory.NewMutable(options);
    }

    // NewComputed

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        T initialValue,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
    {
        var options = new ComputedState<T>.Options() {
            InitialValue = initialValue,
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
        Result<T> initialValue,
        IUpdateDelayer updateDelayer,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer)
    {
        var options = new ComputedState<T>.Options() {
            InitialValue = initialValue,
            UpdateDelayer = updateDelayer,
        };
        return factory.NewComputed(options, computer);
    }
}
