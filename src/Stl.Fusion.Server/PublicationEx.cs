using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server
{
    public static class HttpContextEx
    {
        public static void Publish(this HttpContext httpContext, IPublication publication)
        {
            using var _ = publication.Use();
            var state = publication.State;
            var computed = state.Computed;
            var isConsistent = computed.IsConsistent;

            var headers = httpContext.Response.Headers;
            if (headers.ContainsKey(FusionHeaders.PublisherId))
                throw Errors.AlreadyShared();
            headers[FusionHeaders.PublisherId] = publication.Publisher.Id.Value;
            headers[FusionHeaders.PublicationId] = publication.Id.Value;
            headers[FusionHeaders.Version] = state.Computed.Version.ToString();
            if (!isConsistent)
                headers[FusionHeaders.IsConsistent] = isConsistent.ToString();
        }

        public static async Task<IComputed<T>> PublishAsync<T>(
            this HttpContext httpContext, IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var p = await publisher.PublishAsync(producer, cancellationToken).ConfigureAwait(false);
            var c = p.State.Computed;
            httpContext.Publish(p);
            return c;
        }

        public static Task<IComputed<T>> MaybePublishAsync<T>(
            this HttpContext httpContext, IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var headers = httpContext.Request.Headers;
            var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out var _);
            return mustPublish
                ? httpContext.PublishAsync(publisher, producer, cancellationToken)
                : Computed.CaptureAsync(producer, cancellationToken);
        }
    }
}
