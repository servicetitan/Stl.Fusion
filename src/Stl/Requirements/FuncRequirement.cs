using System.Diagnostics.CodeAnalysis;

namespace Stl.Requirements;

public record FuncRequirement<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    (Func<T?, bool> Validator) : CustomizableRequirementBase<T>
{
    public FuncRequirement(ExceptionBuilder exceptionBuilder, Func<T?, bool> validator) : this(validator)
        => ExceptionBuilder = exceptionBuilder;

    public static implicit operator FuncRequirement<T>((ExceptionBuilder ExceptionBuilder, Func<T?, bool> Validator) args)
        => new(args.ExceptionBuilder, args.Validator);
    public static implicit operator FuncRequirement<T>(Func<T?, bool> validator)
        => new(validator);

#if NETSTANDARD2_0
    public override bool IsSatisfied(T? value)
#else
    public override bool IsSatisfied([NotNullWhen(true)] T? value)
#endif
        => Validator.Invoke(value);
}
