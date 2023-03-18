namespace Stl.Fusion;

public static class StateFactoryExt
{
    // NewMutable

    public static IMutableState<T> NewMutable<T>(
        this IStateFactory factory,
        T initialValue = default!,
        string? category = null)
    {
        var options = new MutableState<T>.Options() {
            InitialValue = initialValue,
            Category = category,
        };
        return factory.NewMutable(options);
    }

    public static IMutableState<T> NewMutable<T>(
        this IStateFactory factory,
        Result<T> initialOutput,
        string? category = null)
    {
        var options = new MutableState<T>.Options() {
            InitialOutput = initialOutput,
            Category = category,
        };
        return factory.NewMutable(options);
    }

    // NewComputed

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer,
        string? category = null)
    {
        var options = new ComputedState<T>.Options() {
            Category = category,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        T initialValue,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer,
        string? category = null)
    {
        var options = new ComputedState<T>.Options() {
            InitialValue = initialValue,
            Category = category,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        Result<T> initialOutput,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer,
        string? category = null)
    {
        var options = new ComputedState<T>.Options() {
            InitialOutput = initialOutput,
            Category = category,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        IUpdateDelayer updateDelayer,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer,
        string? category = null)
    {
        var options = new ComputedState<T>.Options() {
            UpdateDelayer = updateDelayer,
            Category = category,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        T initialValue,
        IUpdateDelayer updateDelayer,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer,
        string? category = null)
    {
        var options = new ComputedState<T>.Options() {
            InitialValue = initialValue,
            UpdateDelayer = updateDelayer,
            Category = category,
        };
        return factory.NewComputed(options, computer);
    }

    public static IComputedState<T> NewComputed<T>(
        this IStateFactory factory,
        Result<T> initialOutput,
        IUpdateDelayer updateDelayer,
        Func<IComputedState<T>, CancellationToken, Task<T>> computer,
        string? category = null)
    {
        var options = new ComputedState<T>.Options() {
            InitialOutput = initialOutput,
            UpdateDelayer = updateDelayer,
            Category = category,
        };
        return factory.NewComputed(options, computer);
    }
}
