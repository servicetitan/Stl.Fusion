using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server
{
    public static class HttpContextEx
    {
        public static readonly string PublisherHeaderName = "Fusion-PublisherId"; 
        public static readonly string PublicationHeaderName = "Fusion-PublicationId"; 

        public static void Share(this HttpContext httpContext, IPublication publication)
        {
            var headers = httpContext.Response.Headers;
            if (headers.ContainsKey(PublisherHeaderName))
                throw Errors.AlreadyShared();
            headers[PublisherHeaderName] = publication.Publisher.Id.Value;
            headers[PublicationHeaderName] = publication.Id.Value;
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
