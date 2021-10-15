using Stl.Concurrency;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Internal;

public interface IPublicationFactory
{
    public IPublication Create(Type genericType,
        IPublisher publisher, IComputed computed,
        Symbol publicationId, IMomentClock clock);
}

public sealed class PublicationFactory : IPublicationFactory
{
    private delegate IPublication Constructor(
        IPublisher publisher, IComputed computed,
        Symbol publicationId, IMomentClock clock);

    private static readonly ConcurrentDictionary<Type, Constructor> ConstructorCache = new();
    private static readonly Func<Type, Constructor> CreateCache = Create;

    public static PublicationFactory Instance { get; } = new();

    private PublicationFactory() { }

    public IPublication Create(Type genericType,
        IPublisher publisher, IComputed computed,
        Symbol publicationId, IMomentClock clock)
        => ConstructorCache
            .GetOrAddChecked(genericType, CreateCache)
            .Invoke(publisher, computed, publicationId, clock);

    private static Constructor Create(Type genericType)
    {
        if (!genericType.IsGenericTypeDefinition)
            throw Errors.TypeMustBeOpenGenericType(genericType);

        var handler = new FactoryApplyHandler(genericType);

        IPublication Factory(
            IPublisher publisher, IComputed computed,
            Symbol publicationId, IMomentClock clock)
            => computed.Apply(handler, (publisher, publicationId, clock));

        return Factory;
    }

    private class FactoryApplyHandler : IComputedApplyHandler<
        (IPublisher Publisher, Symbol PublicationId, IMomentClock Clock),
        IPublication>
    {
        private readonly Type _genericType;
        private readonly ConcurrentDictionary<Type, Type> _closedTypeCache = new();

        public FactoryApplyHandler(Type genericType)
            => _genericType = genericType;

        public IPublication Apply<TIn, TOut>(
            IComputed<TIn, TOut> computed,
            (IPublisher Publisher, Symbol PublicationId, IMomentClock Clock) arg)
            where TIn : ComputedInput
        {
            var closedType = _closedTypeCache.GetOrAddChecked(
                typeof(TOut),
                (tArg, tGeneric) => tGeneric.MakeGenericType(tArg),
                _genericType);
            return (IPublication) closedType.CreateInstance(
                _genericType, arg.Publisher,
                (IComputed<TOut>) computed, arg.PublicationId, arg.Clock);
        }
    }
}
