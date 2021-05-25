using System.Threading.Tasks;
#if NETCOREAPP
using Microsoft.AspNetCore.Mvc;
#else
using System.Web;
using System.Web.Http;
#endif
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Services
{
    #if NETCOREAPP
    
    [Route("api/[controller]/[action]")]
    [ApiController, JsonifyErrors]
    public class ScreenshotController : ControllerBase
    {
        public static int CallCount { get; set; }
        protected IScreenshotService Service { get; }

        public ScreenshotController(IScreenshotService service) => Service = service;

        [HttpGet, Publish]
        public Task<Screenshot> GetScreenshot(int width)
        {
            CallCount++;
            return Service.GetScreenshot(width, HttpContext.RequestAborted);
        }
    }

    #else
    
    [JsonifyErrors]
    public class ScreenshotController : ApiController
    {
        public static int CallCount { get; set; }
        protected IScreenshotService Service { get; }

        public ScreenshotController(IScreenshotService service) => Service = service;

        [HttpGet, Publish]
        public Task<Screenshot> GetScreenshot(int width)
        {
            CallCount++;
            return Service.GetScreenshot(width, ActionContext.RequestAborted());
        }
    }
    
    #endif
}
