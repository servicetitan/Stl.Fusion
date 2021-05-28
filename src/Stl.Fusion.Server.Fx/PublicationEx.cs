using System;
using System.Collections.Generic;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using System.Web.Http.Controllers;
using System.Web.Http.Dependencies;
using Newtonsoft.Json;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Server.Internal;

namespace Stl.Fusion.Server
{
    public static class HttpContextEx
    {
        public static T GetRequiredService<T>(this IDependencyScope dependencyScope)
        {
            var service = GetService<T>(dependencyScope);
            if (service == null)
                throw new InvalidOperationException($"Required service '{typeof(T)}' is not registered.");
            return service;
        }
        
        public static T GetService<T>(this IDependencyScope dependencyScope)
        {
            var service = (T)dependencyScope.GetService(typeof(T));
            return service; 
        }
        
        public static IDependencyScope GetAppServices(this HttpActionContext httpContext)
        {
            var appServices = httpContext.RequestContext.Configuration.DependencyResolver;
            return appServices;
        }
        
        public static IPublicationState? GetPublicationState(this HttpActionContext httpContext)
        {
            if (!httpContext.GetItems().TryGetValue(typeof(IPublicationState), out var v))
                return null;
            return (IPublicationState?)v;
        }

        public static PublicationStateInfo? GetPublicationStateInfo(this HttpActionContext httpContext)
        {
            if (!httpContext.GetItems().TryGetValue(typeof(PublicationStateInfo), out var v))
                return null;
            return (PublicationStateInfo?)v;
        }

        public static IDictionary<object, object> GetItems(this HttpActionContext httpContext)
        {
            // https://stackoverflow.com/questions/18690500/how-to-store-global-per-request-data-in-a-net-web-api-project
            
            var key = "$request_items$";
            var requestProperties = httpContext.Request.Properties;
            IDictionary<object, object> items = null;
            if (requestProperties.TryGetValue(key, out var obj)) {
                items = obj as IDictionary<object,object>;
                if (items == null && obj != null)
                    throw new InvalidOperationException("$request_items$ key is in use for something else");
            }
            if (items == null) {
                items = new Dictionary<object, object>();
                requestProperties[key] = items;
            }
            return items;
        }

        public static void Publish(this HttpActionContext httpContext, IPublication publication)
        {
            using var _ = publication.Use();
            var publicationState = publication.State;
            var computed = publicationState.Computed;
            var isConsistent = computed.IsConsistent();

            // If exception occurred, response is empty, and we can not assign publication header.
            // If JsonifyAttribute is used, it will do it.
            var responseHeaders = httpContext.Response?.Headers;
            if (responseHeaders!=null && responseHeaders.Contains(FusionHeaders.Publication))
                throw Errors.AlreadyPublished();
            var psi = new PublicationStateInfo(publication.Ref, computed.Version, isConsistent);
            
            var items = httpContext.GetItems();
            items[typeof(IPublicationState)] = publicationState;
            items[typeof(PublicationStateInfo)] = psi;
            
            if (responseHeaders!=null)
                responseHeaders.AddPublicationStateInfoHeader(psi);
        }

        internal static bool AddPublicationStateInfoHeader(this HttpResponseHeaders responseHeaders, PublicationStateInfo psi)
        {
            if (responseHeaders.Contains(FusionHeaders.Publication))
                return false;
            responseHeaders.Add(FusionHeaders.Publication, JsonConvert.SerializeObject(psi));
            return true;
        }

        public static async Task<IComputed<T>> Publish<T>(
            this HttpActionContext httpActionContext,
            IPublisher publisher,
            Func<CancellationToken, Task<T>> producer,
            CancellationToken cancellationToken = default)
        {
            var p = await publisher.Publish(producer, cancellationToken).ConfigureAwait(false);
            var c = p.State.Computed;
            httpActionContext.Publish(p);
            return c;
        }
    }
}
