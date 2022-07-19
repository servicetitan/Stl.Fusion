using System.ComponentModel.DataAnnotations;

namespace Stl.Requirements;

public record MustExistRequirement<T> : CustomizableRequirementBase<T>
{
    public static MustExistRequirement<T> Instance { get; } = new();

    public MustExistRequirement()
        => ExceptionBuilder = new("'{0}' is not found.", message => new ValidationException(message));

    public override bool IsSatisfied(T? value)
        => typeof(T).IsValueType
            ? !EqualityComparer<T>.Default.Equals(value!, default!)
            : !ReferenceEquals(value, null);
}
