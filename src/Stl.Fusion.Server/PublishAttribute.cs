using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;
using Stl.Fusion.Internal;

namespace Stl.Fusion.Server
{
    public class PublishAttribute : ActionFilterAttribute, IAsyncExceptionFilter
    {
        public bool RewriteErrors { get; set; } = true;

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

        public override Task OnResultExecutionAsync(ResultExecutingContext context, ResultExecutionDelegate next)
        {
            var httpContext = context.HttpContext;
            var publicationState = httpContext.GetPublicationState();
            var publication = publicationState?.Publication;
            if (publication != null) {
                switch (context.Result) {
                case ObjectResult objectResult:
                    publication.ThrowIfIncompatibleResult(objectResult.Value);
                    break;
                case ContentResult contentResult:
                    publication.ThrowIfIncompatibleResult("");
                    break;
                }
            }
            return base.OnResultExecutionAsync(context, next);
        }

        public Task OnExceptionAsync(ExceptionContext context)
        {
            var jsonExceptionInfoFilter = new JsonifyErrorsAttribute() {
                RewriteErrors = RewriteErrors
            };
            return jsonExceptionInfoFilter.OnExceptionAsync(context);
        }
    }
}
