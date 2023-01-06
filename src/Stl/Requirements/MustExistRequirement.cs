using System.ComponentModel.DataAnnotations;
using System.Diagnostics.CodeAnalysis;

namespace Stl.Requirements;

public record MustExistRequirement<T> : CustomizableRequirementBase<T>
{
    public static MustExistRequirement<T> Default { get; } = new();

    public MustExistRequirement()
        => ExceptionBuilder = new("'{0}' is not found.", message => new ValidationException(message));

#if NETSTANDARD2_0
    public override bool IsSatisfied(T? value)
#else
    public override bool IsSatisfied([NotNullWhen(true)] T? value)
#endif
        => typeof(T).IsValueType
            ? !EqualityComparer<T>.Default.Equals(value!, default!)
            : !ReferenceEquals(value, null);
}
