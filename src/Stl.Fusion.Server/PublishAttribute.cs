using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;

namespace Stl.Fusion.Server
{
    public class PublishAttribute : ActionFilterAttribute
    {
        public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var headers = httpContext.Request.Headers;
            var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out var _);
            if (!mustPublish) {
                await next.Invoke().ConfigureAwait(false);
                return;
            }

            var publisher = httpContext.RequestServices.GetRequiredService<IPublisher>();
            var publication = await publisher
                .PublishAsync(ct => (Task) (next.Invoke()), httpContext.RequestAborted)
                .ConfigureAwait(false);
            httpContext.Publish(publication);
        }
    }
}
