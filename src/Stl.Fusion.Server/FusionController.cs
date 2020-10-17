using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;

namespace Stl.Fusion.Server
{
    [JsonifyErrors]
    public abstract class FusionController : Controller
    {
        protected virtual Task<T> PublishAsync<T>(Func<CancellationToken, Task<T>> producer)
            => PublishAsync(producer, HttpContext.RequestAborted);

        protected virtual Task<T> PublishAsync<T>(Func<CancellationToken, Task<T>> producer, CancellationToken cancellationToken)
        {
            var headers = HttpContext.Request.Headers;
            var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out var _);
            if (!mustPublish)
                return producer.Invoke(cancellationToken);
            var publisher = HttpContext.RequestServices.GetRequiredService<IPublisher>();
            return publisher
                .PublishAsync(producer, cancellationToken)
                .ContinueWith(task => {
                    var publication = task.Result;
                    HttpContext.Publish(publication);
                    return publication.State.Computed.Value;
                }, cancellationToken);
        }
    }
}
