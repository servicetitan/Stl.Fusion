using System;
using System.Threading.Channels;
using Autofac;
using Autofac.Builder;
using Autofac.Extras.DynamicProxy;
using Stl.Channels;
using Stl.Concurrency;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Security;
using Stl.Text;

namespace Stl.Fusion.Autofac
{
    public static class ContainerBuilderEx
    {
        public static ContainerBuilder AddFusion(this ContainerBuilder builder)
        {
            builder.Register(c => ConcurrentIdGenerator.NewInt32())
                .SingleInstance();
            builder.Register(c => ComputedRegistry.Default)
                .As<IComputedRegistry>().SingleInstance();
            builder.Register(c => ArgumentComparerProvider.Default)
                .SingleInstance();
            builder.Register(c => ComputeRetryPolicy.Default)
                .SingleInstance();
            builder.RegisterType<ComputedInterceptor>()
                .SingleInstance();
            builder.RegisterType<CustomFunction>()
                .ComputedProvider();
            return builder;
        }

        public static ContainerBuilder AddFusionPublisher(
            this ContainerBuilder builder, 
            Symbol publisherId, 
            IGenerator<Symbol>? publicationIdGenerator = null) 
        {
            IGenerator<Symbol> PublicationIdGeneratorResolver(IComponentContext c) 
                => publicationIdGenerator ?? c.Resolve<IGenerator<Symbol>>();

            var channelHub = new ChannelHub<PublicationMessage>();
            builder.Register(c => new Publisher(publisherId, channelHub, PublicationIdGeneratorResolver(c))) 
                .As<IPublisher>()
                .SingleInstance();
            return builder;
        }

        public static ContainerBuilder AddFusionReplicator(
            this ContainerBuilder builder,
            Func<Channel<PublicationMessage>, Symbol> publisherIdProvider) 
        {
            var channelHub = new ChannelHub<PublicationMessage>();
            builder.Register(c => new Replicator(channelHub, publisherIdProvider))
                .As<IReplicator>()
                .SingleInstance();
            return builder;
        }

        public static IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> 
            ComputedProvider<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle>(
            this IRegistrationBuilder<TLimit, TConcreteReflectionActivatorData, TRegistrationStyle> registration)
            where TConcreteReflectionActivatorData : ConcreteReflectionActivatorData
        {
            return registration
                .EnableClassInterceptors()
                .InterceptedBy(typeof(ComputedInterceptor))
                .SingleInstance();
        }
    }
}
