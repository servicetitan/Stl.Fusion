using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Stl.Fusion.Internal;
using Stl.Text;

namespace Stl.Fusion.Bridge
{
    public static class PublisherEx
    {
        public static IPublication Get(
            this IPublisher publisher, Symbol publicationId) 
            => publisher.TryGet(publicationId) ?? throw new KeyNotFoundException();

        public static async Task<(IPublication<T> Publication, IComputed<T> Computed)> 
            PublishAsync<T>(this IPublisher publisher, Func<Task<T>> producer)
        {
            using var ccs = ComputeContext.New(ComputeOptions.Capture).Activate();
            await producer.Invoke().ConfigureAwait(false);
            var computed = ccs.Context.GetCapturedComputed<T>();
            if (computed == null)
                throw Errors.NoComputedCaptured();
            var publication = (IPublication<T>) publisher.Publish(computed);
            return (publication, computed);
        }
    }
}
