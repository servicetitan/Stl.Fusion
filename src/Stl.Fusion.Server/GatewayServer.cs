using System.IO;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Stl.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Server.Internal;
using Stl.Fusion.Server.Messages;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public class GatewayServer
    {
        public class Options : IOptions
        {
            public string RequestPath { get; set; } = "/fusion/gw";
            public ITypedSerializer<GatewayMessage, string> Deserializer { get; set; } =
                new SafeJsonNetSerializer(t => typeof(GatewayMessage).IsAssignableFrom(t)).ToTyped<GatewayMessage>();
        }

        public string RequestPath { get; }
        public ITypedSerializer<GatewayMessage, string> Deserializer { get; }
        protected ILogger Log { get; }
        protected IPublisher Publisher { get; }

        public GatewayServer(Options options, IPublisher publisher, ILogger<GatewayServer>? log = null)
        {
            Log = log ??= NullLogger<GatewayServer>.Instance;
            RequestPath = options.RequestPath;
            Deserializer = options.Deserializer;
            Publisher = publisher;
        }

        public async Task HandleAsync(HttpContext context)
        {
            var request = context.Request;
            if (HttpRequestEx.HasJsonContentType(request))
                throw Errors.UnknownContentType(request.ContentType);
            using var r = new StreamReader(request.Body);
            var body = await r.ReadToEndAsync();
            var message = Deserializer.Deserialize(body);
        }
    }
}
