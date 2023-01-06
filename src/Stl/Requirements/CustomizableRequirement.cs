using System.Diagnostics.CodeAnalysis;

namespace Stl.Requirements;

public record CustomizableRequirement<T>(Requirement<T> BaseRequirement) : CustomizableRequirementBase<T>
{
    public CustomizableRequirement(Requirement<T> baseRequirement, ExceptionBuilder exceptionBuilder)
        : this(baseRequirement)
        => ExceptionBuilder = exceptionBuilder;

#if NETSTANDARD2_0
    public override bool IsSatisfied(T? value)
#else
    public override bool IsSatisfied([NotNullWhen(true)] T? value)
#endif
        => BaseRequirement.IsSatisfied(value);
}
