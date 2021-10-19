using Castle.DynamicProxy;

namespace Stl.Fusion.Interception;

public class ArgumentHandler
{
    public static ArgumentHandler Default { get; } = new();

    public Func<object?, int> GetHashCodeFunc { get; init; } =
        o => o?.GetHashCode() ?? 0;
    public Func<object?, object?, bool> EqualsFunc { get; init; } =
        (objA, objB) => objA == objB || (objA?.Equals(objB) ?? false);
    public Func<object?, string> ToStringFunc { get; init; } =
        o => o?.ToString() ?? "␀";
    public Action<ComputeMethodDef, IInvocation, int>? PreprocessFunc { get; init; } = null;
}
