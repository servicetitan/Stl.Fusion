using System;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Web.Http.Filters;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public class JsonifyErrorsAttribute : ExceptionFilterAttribute
    {
        public bool RewriteErrors { get; set; }

        public override void OnException(HttpActionExecutedContext actionExecutedContext)
        {
            var exception = actionExecutedContext.Exception;
            var actionContext = actionExecutedContext.ActionContext;
            var appServices = actionContext.GetAppServices();

            if (actionExecutedContext.Response!=null)
                return; // response already setup, log, do nothing

            if (RewriteErrors) {
                var rewriter = appServices.GetRequiredService<IErrorRewriter>();
                exception = rewriter.Rewrite(actionContext, exception, true);
            }
            var serializer = TypeDecoratingSerializer.Default;
            var content = serializer.Write(new ExceptionParcel(exception));
            actionExecutedContext.Exception = null; // mark exception as handled;
            var response = new HttpResponseMessage {
                Content = new StringContent(content, null, "application/json"),
                StatusCode = HttpStatusCode.InternalServerError
            };
            var psi = actionContext.GetPublicationStateInfo();
            if (psi!=null)
                response.Headers.AddPublicationStateInfoHeader(psi);

            actionExecutedContext.Response = response;
        }

    }
}
