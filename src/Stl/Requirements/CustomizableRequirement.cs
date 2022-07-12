namespace Stl.Requirements;

public record CustomizableRequirement<T>(Requirement<T> BaseRequirement) : CustomizableRequirementBase<T>
{
    public CustomizableRequirement(Requirement<T> baseRequirement, ExceptionBuilder exceptionBuilder)
        : this(baseRequirement)
        => ExceptionBuilder = exceptionBuilder;

    public override bool IsSatisfied(T? value)
        => BaseRequirement.IsSatisfied(value);
}
