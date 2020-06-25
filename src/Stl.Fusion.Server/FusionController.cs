using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server
{
    public abstract class FusionController : Controller
    {
        protected IPublisher Publisher { get; set; }

        protected FusionController(IPublisher publisher) 
            => Publisher = publisher;
    }
}
