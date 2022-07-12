namespace Stl.Requirements;

public abstract record Requirement
{
    public abstract bool IsSatisfiedUntyped(object? value);
    public abstract object RequireUntyped(object? value);
}

public abstract record Requirement<T> : Requirement
{
    public override bool IsSatisfiedUntyped(object? value) 
        => IsSatisfied((T?) value);
    public override object RequireUntyped(object? value)
#pragma warning disable CS8603
        => Require((T?) value);
#pragma warning restore CS8603

    public abstract bool IsSatisfied(T? value);
    public abstract T Require(T? value);

    public static implicit operator Requirement<T>((ExceptionBuilder ExceptionBuilder, Func<T?, bool> Validator) args)
        => FuncRequirement.New(args.ExceptionBuilder, args.Validator);
    public static implicit operator Requirement<T>(Func<T?, bool> validator)
        => FuncRequirement.New(validator);

    public static Requirement<T> operator &(Requirement<T> primary, Requirement<T> secondary)
        => new JointRequirement<T>(primary, secondary);
}

