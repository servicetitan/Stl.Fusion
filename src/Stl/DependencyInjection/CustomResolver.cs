using Stl.Internal;

namespace Stl.DependencyInjection;

public sealed class CustomResolver
{
    public Type Type { get; }
    public Func<IServiceProvider, object>? Resolver { get; }

    public static CustomResolver New(Type type, Func<IServiceProvider, object>? resolver = null)
        => new(type, resolver);
    public static CustomResolver New<TService>(Func<IServiceProvider, TService>? resolver = null)
        where TService : class
        => new(typeof(TService), resolver);

    private CustomResolver(Type type, Func<IServiceProvider, object>? resolver)
    {
        if (type.IsValueType)
            throw Errors.MustBeClass(type, nameof(type));

        Type = type;
        Resolver = resolver;
    }

    public override string ToString()
        => Resolver == null
            ? $"{nameof(CustomResolver)}({Type.GetName()})"
            : $"{nameof(CustomResolver)}({Type.GetName()} -> custom)";

    public static implicit operator CustomResolver(Type type) => New(type);
}
