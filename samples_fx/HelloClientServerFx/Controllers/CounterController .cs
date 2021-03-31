using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http;
using Stl.Fusion.Server;

namespace HelloClientServerFx
{
    // We need Web API controller to publish the service
    //[Route("api/[controller]/[action]")]
    //[ApiController, JsonifyErrors]
    public class CounterController
        : ApiController
        //: ControllerBase
    {
        private ICounterService Counters { get; }

        public CounterController(ICounterService counterService)
            => Counters = counterService;

        // Publish ensures GetAsync output is published if publication was requested by the client:
        // - Publication is created
        // - Its Id is shared in response header.
        [HttpGet, Publish]
        [Route("api/counter/{key}")]
        public Task<int> Get(string key)
        {
            key = key ?? ""; // Empty value is bound to null value by default
            Console.WriteLine($"{GetType().Name}.{nameof(Get)}({key})");
            return Counters.Get(key, CancellationToken.None);
        }

        [HttpPost]
        public Task Increment(string key)
        {
            key = key ?? ""; // Empty value is bound to null value by default
            Console.WriteLine($"{GetType().Name}.{nameof(Increment)}({key})");
            return Counters.Increment(key, CancellationToken.None);
        }

        [HttpPost]
        public Task SetOffset(int offset)
        {
            Console.WriteLine($"{GetType().Name}.{nameof(SetOffset)}({offset})");
            return Counters.SetOffset(offset, CancellationToken.None);
        }
    }
}