using System;
using System.Diagnostics;
using System.Linq;
using System.Net.Http;
using System.Reflection;
using System.Threading.Tasks;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Internal;
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
            var headers = response.Headers;
            if (!(headers.TryGetValues(FusionHeaders.PublisherId, out var values) && values.Any()))
                // No PublisherId -> not a publication / replica
                return InnerDeserialize<T>(content, response, info);
            
            var handler = DeserializeComputedHandlerProvider.Instance[typeof(T)];
            var result = handler.Handle(this, new DeserializeMethodArgs(content, response, info));
            if (result is null)
                return (T) result!;
            if (result is T value) // T is IComputed<?>
                return value;
            return (T) ((IComputed) result).Value!;
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
                var value = InnerDeserialize<T>(content, response, info);
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
            ReplicaCapture.Capture(replica);
            return replica.Computed;
        }

        protected T InnerDeserialize<T>(string? content, HttpResponseMessage response, ResponseDeserializerInfo info)
        {
            // I was assuming ResponseDeserializer is also used for string results,
            // but no, it doesn't. That's why the code below is commented out.
            // var returnType = info.RequestInfo.MethodInfo.ReturnType;
            // if (returnType == typeof(Task<string>) || returnType == typeof(ValueTask<string>))
            //     return (T) (object) (content ?? "");
            return InnerDeserializer.Deserialize<T>(content, response, info);
        }
    }
}
