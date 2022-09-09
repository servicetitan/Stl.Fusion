using Microsoft.AspNetCore.Mvc.Filters;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server;

public class PublishAttribute : ActionFilterAttribute
{
    public override async Task OnActionExecutionAsync(ActionExecutingContext context, ActionExecutionDelegate next)
    {
        var httpContext = context.HttpContext;
        var headers = httpContext.Request.Headers;
        var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out _);
        if (!mustPublish) {
            await next().ConfigureAwait(false);
            return;
        }

        var publisher = httpContext.RequestServices.GetRequiredService<IPublisher>();
        var publication = await publisher
            .Publish(() => (Task) next(), httpContext.RequestAborted)
            .ConfigureAwait(false);
        httpContext.Publish(publication);
    }
}
