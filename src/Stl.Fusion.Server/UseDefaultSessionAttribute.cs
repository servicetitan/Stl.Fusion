using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Server;

public class UseDefaultSessionAttribute : ActionFilterAttribute
{
    public override Task OnActionExecutionAsync(
        ActionExecutingContext context, 
        ActionExecutionDelegate next)
    {
        if (context.HttpContext.Request.Method != HttpMethod.Post.Method)
            return base.OnActionExecutionAsync(context, next);
        foreach (var (_, argument) in context.ActionArguments) {
            if (argument is ISessionCommand command && command.Session.IsDefault()) {
                var httpContext = context.HttpContext;
                var services = httpContext.RequestServices;
                var sessionResolver = services.GetRequiredService<ISessionResolver>();
                command.UseDefaultSession(sessionResolver);
            }
        }
        return base.OnActionExecutionAsync(context, next);
    }
}
