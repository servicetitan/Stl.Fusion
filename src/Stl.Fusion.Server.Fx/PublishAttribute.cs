using System;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.Filters;
using Stl.Fusion.Bridge;
using Stl.Fusion.Client;

namespace Stl.Fusion.Server
{
    public class PublishAttribute : ActionFilterAttribute
    {
        public override void OnActionExecuting(HttpActionContext actionContext)
        {
            base.OnActionExecuting(actionContext);
            
            var request = actionContext.Request;
            var headers = request.Headers;
            var mustPublish = headers.TryGetValues(FusionHeaders.RequestPublication, out var _);
            if (!mustPublish)
                return;

            var items = actionContext.GetItems();
            var ccs = Computed.BeginCapture();
            items.Add(typeof(ComputeContextScope), ccs);
        }

        public override void OnActionExecuted(HttpActionExecutedContext actionExecutedContext)
        {
            var actionContext = actionExecutedContext.ActionContext;
            var items = actionContext.GetItems();
            if (items.TryGetValue(typeof(ComputeContextScope), out var obj) && obj is ComputeContextScope ccs) {
                var wasError = actionExecutedContext.Exception != null;
                var computed = ccs.CompleteCapture(wasError);
                
                var appServices = actionContext.GetAppServices();
                var publisher = appServices.GetService<IPublisher>();
                var publication = publisher.Publish(computed);
                publication.Update(CancellationToken.None); // Надо ли делать Update и как правильно его вызвать?
                actionContext.Publish(publication);
            }
            
            base.OnActionExecuted(actionExecutedContext);
        }
    }
}
