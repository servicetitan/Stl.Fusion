using System.Diagnostics.CodeAnalysis;
using System.Net;
using System.Net.Http;
using System.Web.Http.Filters;
using Stl.Internal;

namespace Stl.Fusion.Server;

public sealed class JsonifyErrorsAttribute : ExceptionFilterAttribute
{
    [RequiresUnreferencedCode(UnreferencedCode.Serialization)]
#pragma warning disable IL2046
    public override void OnException(HttpActionExecutedContext actionExecutedContext)
#pragma warning restore IL2046
    {
        var exception = actionExecutedContext.Exception;
        var actionContext = actionExecutedContext.ActionContext;
        var services = actionContext.GetAppServices();

        if (actionExecutedContext.Response != null)
            return; // response already setup, log, do nothing

        var log = services.GetRequiredService<ILogger<JsonifyErrorsAttribute>>();
        log.LogError(exception, "Error message: {Message}", exception.Message);

        var serializer = TypeDecoratingTextSerializer.Default;
        var content = serializer.Write(exception.ToExceptionInfo());
        actionExecutedContext.Exception = null; // mark exception as handled;
        var response = new HttpResponseMessage {
            Content = new StringContent(content, null, "application/json"),
            StatusCode = HttpStatusCode.InternalServerError
        };
        actionExecutedContext.Response = response;
    }
}
