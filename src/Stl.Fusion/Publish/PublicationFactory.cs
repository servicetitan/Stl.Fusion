using System;
using System.Collections.Concurrent;
using Stl.Fusion.Internal;
using Stl.Reflection;
using Stl.Text;

namespace Stl.Fusion.Publish
{
    public delegate IPublication PublicationFactory(IPublisher publisher, IComputed computed, Symbol publicationId);

    public static class PublicationFactoryEx
    {
        private static readonly ConcurrentDictionary<Type, PublicationFactory> _cache =
            new ConcurrentDictionary<Type, PublicationFactory>();
        private static readonly Func<Type, PublicationFactory> _createHandler = Create;

        public static PublicationFactory For<TPublication>()
            => _cache.GetOrAddChecked(typeof(TPublication), _createHandler);
        public static PublicationFactory For(Type publicationType)
            => _cache.GetOrAddChecked(publicationType, _createHandler);
        public static readonly PublicationFactory Updating = 
            For(typeof(UpdatingPublication<>)); 
        public static readonly PublicationFactory NonUpdating = 
            For(typeof(NonUpdatingPublication<>)); 

        private static PublicationFactory Create(Type publicationType)
        {
            if (!publicationType.IsGenericTypeDefinition) {
                // Publication type is a specific one, so we assume it's "untyped" publication impl.

                IPublication RegularFactory(IPublisher publisher, IComputed computed, Symbol publicationId)
                    => (IPublication) publicationType.CreateInstance(publisher, computed, publicationId);

                return RegularFactory;
            }

            // Publication type is a generic one, so we assume it's "typed" publication impl.
                
            var handler = new FactoryApplyHandler(publicationType);
                
            IPublication GenericFactory(IPublisher publisher, IComputed computed, Symbol publicationId) 
                => computed.Apply(handler, (publisher, publicationId));

            return GenericFactory;
        }

        private class FactoryApplyHandler : IComputedApplyHandler<(IPublisher Publisher, Symbol PublicationId), IPublication>
        {
            private readonly Type _publicationGenericType;
            private readonly ConcurrentDictionary<Type, Type> _publicationTypeCache =
                new ConcurrentDictionary<Type, Type>();

            public FactoryApplyHandler(Type type) => _publicationGenericType = type;

            public IPublication Apply<TIn, TOut>(
                IComputed<TIn, TOut> computed, 
                (IPublisher Publisher, Symbol PublicationId) arg) 
                where TIn : ComputedInput
            {
                var publisherType = _publicationTypeCache.GetOrAddChecked(
                    typeof(TOut), 
                    (tArg, tGeneric) => tGeneric.MakeGenericType(tArg), 
                    _publicationGenericType);
                return (IPublication) publisherType.CreateInstance(
                    arg.Publisher, (IComputed<TOut>) computed, arg.PublicationId);
            }
        }
    }
}
