using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;

namespace Stl.Fusion.Server;

public class JsonifyErrorsAttribute : ExceptionFilterAttribute
{
    public override Task OnExceptionAsync(ExceptionContext context)
    {
        var exception = context.Exception;
        var httpContext = context.HttpContext;
        var services = httpContext.RequestServices;

        var log = services.GetRequiredService<ILogger<JsonifyErrorsAttribute>>();
        log.LogError(exception, "Error message: {Message}", exception.Message);

        var serializer = TypeDecoratingSerializer.Default;
        var content = serializer.Write(exception.ToExceptionInfo());
        var result = new ContentResult() {
            Content = content,
            ContentType = "application/json",
            StatusCode = (int)HttpStatusCode.InternalServerError,
        };
        context.ExceptionHandled = true;
        return result.ExecuteResultAsync(context);
    }
}
