using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Stl.Fusion.Bridge;

namespace Stl.Fusion.Server;

public class PublishAttribute : ActionFilterAttribute
{
    public override void OnActionExecuting(HttpActionContext actionContext)
    {
        base.OnActionExecuting(actionContext);

        var request = actionContext.Request;
        var headers = request.Headers;
        var mustPublish = headers.TryGetValues(FusionHeaders.RequestPublication, out _);
        if (!mustPublish)
            return;

        var items = actionContext.GetItems();
        var computeContextScope = Computed.BeginCapture();
        items.Add(typeof(ComputeContextScope), computeContextScope);
    }

    public override async Task OnActionExecutedAsync(HttpActionExecutedContext actionExecutedContext, CancellationToken cancellationToken)
    {
        var actionContext = actionExecutedContext.ActionContext;
        var items = actionContext.GetItems();
        if (items.TryGetValue(typeof(ComputeContextScope), out var obj) && obj is ComputeContextScope computeContextScope) {
            computeContextScope.Dispose();
            var computed = computeContextScope.Context.GetCaptured();

            var appServices = actionContext.GetAppServices();
            var publisher = appServices.GetRequiredService<IPublisher>();
            var publication = publisher.Publish(computed);
            // Publication doesn't have to be "in sync" with the computed
            // we requested it for (i.e. it might still point to its older,
            // inconsistent version), so we have to update it here.
            try {
                await publication.Update(cancellationToken).ConfigureAwait(false);
            }
            catch (OperationCanceledException) {
                throw;
            }
            catch {
                // Intended, it's fine to publish a computed w/ an error
            }
            actionContext.Publish(publication);
        }

        await base.OnActionExecutedAsync(actionExecutedContext, cancellationToken).ConfigureAwait(false);
    }
}
