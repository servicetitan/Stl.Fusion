using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion;
using Stl.Fusion.Bridge;
using Stl.Fusion.Bridge.Messages;
using Stl.Samples.Blazor.Common.Services;

namespace Stl.Samples.Blazor.Server.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class TimeController : Controller
    {
        protected ITimeProvider TimeProvider { get; }
        protected IPublisher Publisher { get; }

        public TimeController(
            ITimeProvider timeProvider,
            IPublisher publisher)
        {
            TimeProvider = timeProvider;
            Publisher = publisher;
        }

        [HttpGet]
        public async Task<ActionResult<PublicationPublishedMessage>> GetTime()
        {
            var publication = await Computed
                .PublishAsync(Publisher, () => TimeProvider.GetTimeAsync(100))
                .ConfigureAwait(false);

            // As you see, the value isn't even sent.
            // In future we will be sending it, but for now let's just send
            // the bare minimum - the client will get the update via
            // WebSocket channel anyway.
            return new PublicationPublishedMessage() {
                PublicationId = publication.Id,
                PublisherId = Publisher.Id,
            };
        }
    }
}
