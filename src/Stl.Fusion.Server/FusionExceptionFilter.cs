using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Stl.Fusion.Client;

namespace Stl.Fusion.Server
{
    public class FusionExceptionFilter : ExceptionFilterAttribute
    {
        public override Task OnExceptionAsync(ExceptionContext context)
        {
            var httpContext = context.HttpContext;
            var headers = httpContext.Request.Headers;
            var mustPublish = headers.TryGetValue(FusionHeaders.RequestPublication, out var _);
            if (!mustPublish)
                return base.OnExceptionAsync(context);

            var result = new ContentResult() {
                StatusCode = (int) HttpStatusCode.InternalServerError,
                Content = context.Exception.Message,
            };
            return result.ExecuteResultAsync(context);
        }
    }
}
