using System.Diagnostics.CodeAnalysis;
using Stl.Requirements;

namespace Stl;

public abstract record Requirement
{
#if NETSTANDARD2_0
    public abstract bool IsSatisfiedUntyped(object? value);
    public abstract object CheckUntyped(object? value);
#else
    public abstract bool IsSatisfiedUntyped([NotNullWhen(true)] object? value);
    public abstract object CheckUntyped([NotNull] object? value);
#endif

    public static FuncRequirement<T> New<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (ExceptionBuilder exceptionBuilder, Func<T?, bool> validator)
        => new(exceptionBuilder, validator);
    public static FuncRequirement<T> New<
        [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
        (Func<T?, bool> validator)
        => new(validator);
}

public abstract record Requirement<
    [DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.All)] T>
    : Requirement
{
    private const string MustExistFieldOrPropertyName = "MustExist";
    // ReSharper disable once StaticMemberInGenericType
    private static readonly object MustExistLock = new();
    private static volatile Requirement<T>? _mustExist;

    public static Requirement<T> MustExist {
        get {
            if (_mustExist != null)
                return _mustExist;
            lock (MustExistLock) {
                if (_mustExist != null)
                    return _mustExist;

                var type = typeof(T);
                var result = type
                    .GetField(MustExistFieldOrPropertyName, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null) as Requirement<T>;
                result ??= type
                    .GetProperty(MustExistFieldOrPropertyName, BindingFlags.Public | BindingFlags.Static)
                    ?.GetValue(null) as Requirement<T>;
                result ??= MustExistRequirement<T>.Default;
                return _mustExist = result;
            }
        }
    }

#if NETSTANDARD2_0
    public override bool IsSatisfiedUntyped(object? value)
        => IsSatisfied((T?) value);
    public override object CheckUntyped(object? value)
        => Check((T?) value)!;

    public abstract bool IsSatisfied(T? value);
    public abstract T Check(T? value);
#else
    public override bool IsSatisfiedUntyped([NotNullWhen(true)] object? value)
        => IsSatisfied((T?) value);
    public override object CheckUntyped([NotNull] object? value)
        => Check((T?) value)!;

    public abstract bool IsSatisfied([NotNullWhen(true)] T? value);
    public abstract T Check([NotNull] T? value);
#endif

    public Requirement<T> And(Requirement<T> secondary)
        => new JointRequirement<T>(this, secondary);

    public Requirement<T> With(ExceptionBuilder exceptionBuilder)
        => this is CustomizableRequirementBase<T> customizableRequirement
            ? customizableRequirement with { ExceptionBuilder = exceptionBuilder }
            : new CustomizableRequirement<T>(this, exceptionBuilder);
    public Requirement<T> With(string messageTemplate, Func<string, Exception>? exceptionFactory = null)
        => With(new ExceptionBuilder(messageTemplate, exceptionFactory));
    public Requirement<T> With(string messageTemplate, string targetName, Func<string, Exception>? exceptionFactory = null)
        => With(new ExceptionBuilder(messageTemplate, targetName, exceptionFactory));
    public Requirement<T> With(Func<Exception> exceptionFactory)
        => With(new ExceptionBuilder(exceptionFactory));

    public static implicit operator Requirement<T>((ExceptionBuilder ExceptionBuilder, Func<T?, bool> Validator) args)
        => New(args.ExceptionBuilder, args.Validator);
    public static implicit operator Requirement<T>(Func<T?, bool> validator)
        => New(validator);

    public static Requirement<T> operator &(Requirement<T> primary, Requirement<T> secondary)
        => primary.And(secondary);
    public static Requirement<T> operator +(Requirement<T> requirement, ExceptionBuilder exceptionBuilder)
        => requirement.With(exceptionBuilder);
    public static Requirement<T> operator +(Requirement<T> requirement, string messageTemplate)
        => requirement.With(messageTemplate);
    public static Requirement<T> operator +(Requirement<T> requirement, Func<Exception> exceptionBuilder)
        => requirement.With(exceptionBuilder);
}
