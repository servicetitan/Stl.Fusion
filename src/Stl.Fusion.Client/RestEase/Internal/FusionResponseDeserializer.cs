using System.Linq;
using System.Net.Http;
using Castle.Core.Internal;
using Newtonsoft.Json;
using RestEase;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Internal;

namespace Stl.Fusion.Client.RestEase.Internal
{
    public class FusionResponseDeserializer : ResponseDeserializer
    {
        protected IReplicator Replicator { get; }
        protected ResponseDeserializer InnerDeserializer { get; }

        public FusionResponseDeserializer(
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
            if (headers.TryGetValues(FusionHeaders.Publication, out var values)) {
                var header = values.FirstOrDefault();
                if (!header.IsNullOrEmpty()) {
                    var psi = JsonConvert.DeserializeObject<PublicationStateInfo>(header);
                    PublicationStateInfoCapture.Capture(psi);
                }
            }
            return InnerDeserializer.Deserialize<T>(content, response, info);
        }
    }
}
