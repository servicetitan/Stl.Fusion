using System.Diagnostics.CodeAnalysis;
using System.Net;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Stl.Internal;

namespace Stl.Fusion.Server;

public sealed class JsonifyErrorsAttribute : ExceptionFilterAttribute
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public override Task OnExceptionAsync(ExceptionContext context)
#pragma warning restore IL2046
    {
        var exception = context.Exception;
        var httpContext = context.HttpContext;
        var services = httpContext.RequestServices;

        var log = services.GetRequiredService<ILogger<JsonifyErrorsAttribute>>();
        log.LogError(exception, "Error message: {Message}", exception.Message);

        var serializer = TypeDecoratingTextSerializer.Default;
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
