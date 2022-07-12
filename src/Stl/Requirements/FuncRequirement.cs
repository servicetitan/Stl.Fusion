namespace Stl.Requirements;

public record FuncRequirement<T>(Func<T?, bool> Validator) : CustomizableRequirement<T>
{
    public FuncRequirement(ExceptionBuilder exceptionBuilder, Func<T?, bool> validator) : this(validator) 
        => ExceptionBuilder = exceptionBuilder;

    public static implicit operator FuncRequirement<T>((ExceptionBuilder ExceptionBuilder, Func<T?, bool> Validator) args)
        => new(args.ExceptionBuilder, args.Validator);
    public static implicit operator FuncRequirement<T>(Func<T?, bool> validator)
        => new(validator);

    public override bool IsSatisfied(T? value)
        => Validator.Invoke(value);
}

public static class FuncRequirement
{
    public static FuncRequirement<T> New<T>(ExceptionBuilder exceptionBuilder, Func<T?, bool> validator) =>
        new(exceptionBuilder, validator);
    public static FuncRequirement<T> New<T>(Func<T?, bool> validator) =>
        new(validator);
}
