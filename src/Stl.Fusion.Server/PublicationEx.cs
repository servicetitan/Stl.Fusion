using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server
{
    public static class HttpContextEx
    {
        public static void Share(this HttpContext httpContext, IPublication publication)
        {
            using var _ = publication.Use();
            var state = publication.State;
            var computed = state.Computed;
            var isConsistent = computed.IsConsistent;

            var headers = httpContext.Response.Headers;
            if (headers.ContainsKey(HttpHeaderNames.PublisherId))
                throw Errors.AlreadyShared();
            headers[HttpHeaderNames.PublisherId] = publication.Publisher.Id.Value;
            headers[HttpHeaderNames.PublicationId] = publication.Id.Value;
            headers[HttpHeaderNames.LTag] = state.Computed.LTag.ToString();
            if (!isConsistent)
                headers[HttpHeaderNames.IsConsistent] = isConsistent.ToString();
        }

        public static async Task<IComputed<T>> ShareAsync<T>(
            this HttpContext httpContext, IPublisher publisher, Func<Task<T>> producer)
        {
            var (publication, computed) = await publisher.PublishAsync(producer);
            httpContext.Share(publication);
            return computed;
        }
    }
}
