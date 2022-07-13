using System.ComponentModel.DataAnnotations;

namespace Stl.Requirements;

public record NotNullOrDefaultRequirement<T> : CustomizableRequirementBase<T>
{
    public static NotNullOrDefaultRequirement<T> Default { get; } = new();

    public NotNullOrDefaultRequirement()
        => ExceptionBuilder = new("'{0}' is not found.", message => new ValidationException(message));

    public override bool IsSatisfied(T? value)
        => typeof(T).IsValueType
            ? !EqualityComparer<T>.Default.Equals(value!, default!)
            : !ReferenceEquals(value, null);
}
