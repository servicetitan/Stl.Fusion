using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;

namespace Stl.Fusion.Server
{
    public abstract class FusionController : Controller
    {
        protected IPublisher Publisher { get; set; }

        protected FusionController(IPublisher publisher)
            => Publisher = publisher;

        protected virtual Task<T> PublishAsync<T>(Func<CancellationToken, Task<T>> producer)
        {
            // This method is supposed to be the one you use in most of cases - that's
            // why it doesn't accept CancellationToken & uses the most likely default
            // for it instead. If you'd like to use some other CancellationToken,
            // you should use MaybePublishAsync extension method instead.
            // Note that this method is virtual, so you can override it as well.
            var cancellationToken = HttpContext.RequestAborted;
            var headers = HttpContext.Request.Headers;
            var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out var _);
            if (!mustPublish)
                return producer.Invoke(cancellationToken);
            return Publisher
                .PublishAsync(producer, cancellationToken)
                .ContinueWith(task => {
                    var publication = task.Result;
                    HttpContext.Publish(publication);
                    return publication.State.Computed.Value;
                }, cancellationToken);
        }
    }
}
