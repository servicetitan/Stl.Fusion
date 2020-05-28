using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client.RestEase.Internal;

namespace Stl.Fusion.Client.RestEase
{
    public class ReplicaResponseDeserializer : ResponseDeserializer
    {
        protected IReplicator Replicator { get; }
        protected ResponseDeserializer InnerDeserializer { get; }

        public ReplicaResponseDeserializer(
            IReplicator replicator,
            ResponseDeserializer innerDeserializer)
        {
            Replicator = replicator;
            InnerDeserializer = innerDeserializer;
        }

        public override T Deserialize<T>(
            string? content, HttpResponseMessage response, 
            ResponseDeserializerInfo info)
        {
            var handler = DeserializeComputedHandlerProvider.Instance[typeof(T)];
            var computed = handler.Handle(this, new DeserializeMethodArgs(content, response, info));
            if (computed != null)
                return (T) computed;
            return InnerDeserializer.Deserialize<T>(content, response, info);
        }

        public virtual IComputed<T> DeserializeComputed<T>(
            string? content, HttpResponseMessage response, 
            ResponseDeserializerInfo info)
        {
            Result<T> result;
            if (!response.IsSuccessStatusCode) {
                var message = $"{response.StatusCode}: {response.ReasonPhrase}";
                var error = new TargetInvocationException(message, null);
                result = new Result<T>(default!, error);
            }
            else {
                var value = InnerDeserializer.Deserialize<T>(content, response, info);
                result = new Result<T>(value, null);
            }

            var headers = response.Headers;
            var publisherId = headers.GetValues(FusionHeaders.PublisherId).Single();
            var publicationId = headers.GetValues(FusionHeaders.PublicationId).Single();
            var lTag = LTag.Parse(headers.GetValues(FusionHeaders.LTag).Single());
            var isConsistent = true;
            if (headers.Contains(FusionHeaders.IsConsistent))
                isConsistent = Boolean.Parse(headers.GetValues(FusionHeaders.IsConsistent).Single());
            var lTagged = new LTagged<Result<T>>(result, lTag);
            var replica = Replicator.GetOrAdd(publisherId, publicationId, lTagged, isConsistent);
            return replica.Computed;
        }
    }
}
