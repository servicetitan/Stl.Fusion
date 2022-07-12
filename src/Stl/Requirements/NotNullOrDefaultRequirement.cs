namespace Stl.Requirements;

public record NotNullOrDefaultRequirement<T> : CustomizableRequirement<T>
{
    public static NotNullOrDefaultRequirement<T> Default { get; } = new();

    public NotNullOrDefaultRequirement() 
        => ExceptionBuilder = ("'{0}' is not found.", message => new ObjectNotFoundException(message));

    public override bool IsSatisfied(T? value) 
        => typeof(T).IsValueType
            ? !EqualityComparer<T>.Default.Equals(value!, default!)
            : !ReferenceEquals(value, null);
}
