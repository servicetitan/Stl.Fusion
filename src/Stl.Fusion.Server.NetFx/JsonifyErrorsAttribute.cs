using System.Net;
using System.Web.Http.Filters;

namespace Stl.Fusion.Server;

public class JsonifyErrorsAttribute : ExceptionFilterAttribute
{
    public bool RewriteErrors { get; set; }

    public override void OnException(HttpActionExecutedContext actionExecutedContext)
    {
        var exception = actionExecutedContext.Exception;
        var actionContext = actionExecutedContext.ActionContext;
        var services = actionContext.GetAppServices();

        if (actionExecutedContext.Response != null)
            return; // response already setup, log, do nothing

        if (RewriteErrors) {
            var rewriter = services.GetRequiredService<IErrorRewriter>();
            exception = rewriter.Rewrite(actionContext, exception, true);
        }

        var log = services.GetRequiredService<ILogger<JsonifyErrorsAttribute>>();
        log.LogError(exception, "Error message: {Message}", exception.Message);

        var serializer = TypeDecoratingSerializer.Default;
        var content = serializer.Write(exception.ToExceptionInfo());
        actionExecutedContext.Exception = null; // mark exception as handled;
        var response = new HttpResponseMessage {
            Content = new StringContent(content, null, "application/json"),
            StatusCode = HttpStatusCode.InternalServerError
        };
        var psi = actionContext.GetPublicationStateInfo();
        if (psi != null)
            response.Headers.AddPublicationStateInfoHeader(psi);

        actionExecutedContext.Response = response;
    }
}
