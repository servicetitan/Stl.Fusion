using Stl.Fusion.Internal;

namespace Stl.Fusion.Bridge.Internal;

public interface ISubscriptionProcessorFactory
{
    public SubscriptionProcessor Create(Type genericType,
        IPublication publication, PublisherChannelProcessor publisherChannelProcessor,
        TimeSpan subscriptionExpirationTime, MomentClockSet clocks,
        IServiceProvider services);
}

public sealed class SubscriptionProcessorFactory : ISubscriptionProcessorFactory
{
    private delegate SubscriptionProcessor Constructor(
        IPublication publication, PublisherChannelProcessor publisherChannelProcessor,
        TimeSpan subscriptionExpirationTime, MomentClockSet clocks,
        IServiceProvider services);

    private static readonly ConcurrentDictionary<Type, Constructor> ConstructorCache = new();
    private static readonly Func<Type, Constructor> CreateCache = Create;

    public static SubscriptionProcessorFactory Instance { get; } = new();

    private SubscriptionProcessorFactory() { }

    public SubscriptionProcessor Create(Type genericType,
        IPublication publication, PublisherChannelProcessor publisherChannelProcessor,
        TimeSpan subscriptionExpirationTime, MomentClockSet clocks,
        IServiceProvider services)
        => ConstructorCache
            .GetOrAdd(genericType, CreateCache)
            .Invoke(publication, publisherChannelProcessor, subscriptionExpirationTime, clocks, services);

    private static Constructor Create(Type genericType)
    {
        if (!genericType.IsGenericTypeDefinition)
            throw Errors.TypeMustBeOpenGenericType(genericType);

        var handler = new FactoryApplyHandler(genericType);

        SubscriptionProcessor Factory(
            IPublication publication, PublisherChannelProcessor publisherChannelProcessor,
            TimeSpan subscribeTimeout, MomentClockSet clocks, IServiceProvider services)
            => publication.Apply(handler, (publisherChannelProcessor, subscribeTimeout, clocks, services));

        return Factory;
    }

    private class FactoryApplyHandler : IPublicationApplyHandler<
        (PublisherChannelProcessor PublisherChannelProcessor,
        TimeSpan SubscriptionExpirationTime,
        MomentClockSet Clocks,
        IServiceProvider Services),
        SubscriptionProcessor>
    {
        private readonly Type _genericType;
        private readonly ConcurrentDictionary<Type, Type> _closedTypeCache = new();

        public FactoryApplyHandler(Type genericType)
            => _genericType = genericType;

        public SubscriptionProcessor Apply<T>(
            Publication<T> publication,
            (PublisherChannelProcessor PublisherChannelProcessor,
                TimeSpan SubscriptionExpirationTime,
                MomentClockSet Clocks,
                IServiceProvider Services) arg)
        {
            var closedType = _closedTypeCache.GetOrAdd(
                typeof(T),
                (tArg, tGeneric) => tGeneric.MakeGenericType(tArg),
                _genericType);
            return (SubscriptionProcessor) closedType.CreateInstance(
                publication,
                arg.PublisherChannelProcessor,
                arg.SubscriptionExpirationTime,
                arg.Clocks,
                arg.Services);
        }
    }
}
