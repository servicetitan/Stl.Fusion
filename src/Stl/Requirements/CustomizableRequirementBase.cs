using System.Diagnostics.CodeAnalysis;

namespace Stl.Requirements;

public abstract record CustomizableRequirementBase<T> : Requirement<T>
{
    public ExceptionBuilder ExceptionBuilder { get; init; }

#if NETSTANDARD2_0
    public override T Check(T? value)
#else
    public override T Check([NotNull] T? value)
#endif
    {
        if (!IsSatisfied(value))
            throw ExceptionBuilder.Build(value);
        return value!;
    }
}
