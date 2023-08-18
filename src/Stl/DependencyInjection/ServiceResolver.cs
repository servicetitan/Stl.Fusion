using Stl.Internal;

namespace Stl.DependencyInjection;

public sealed class ServiceResolver
{
    public Type Type { get; }
    public Func<IServiceProvider, object>? Resolver { get; }

    public static ServiceResolver New(Type type, Func<IServiceProvider, object>? resolver = null)
        => new(type, resolver);
    public static ServiceResolver New<TService>(Func<IServiceProvider, TService>? resolver = null)
        where TService : class
        => new(typeof(TService), resolver);

    private ServiceResolver(Type type, Func<IServiceProvider, object>? resolver)
    {
        if (type.IsValueType)
            throw Errors.MustBeClass(type, nameof(type));

        Type = type;
        Resolver = resolver;
    }

    public override string ToString()
        => Resolver == null
            ? Type.GetName()
            : "*" + Type.GetName();

    public static implicit operator ServiceResolver(Type type) => New(type);
}
