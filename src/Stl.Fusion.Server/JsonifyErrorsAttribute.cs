using System;
using System.Net;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Filters;
using Microsoft.Extensions.DependencyInjection;
using Stl.Serialization;

namespace Stl.Fusion.Server
{
    public class JsonifyErrorsAttribute : ExceptionFilterAttribute
    {
        public bool RewriteErrors { get; set; }

        public override Task OnExceptionAsync(ExceptionContext context)
        {
            var exception = context.Exception;
            if (RewriteErrors) {
                var rewriter = context.HttpContext.RequestServices.GetRequiredService<IErrorRewriter>();
                exception = rewriter.Rewrite(context, exception, true);
            }
            var serializer = TypeDecoratingSerializer.Default;
            var content = serializer.Write(exception.ToExceptionInfo());
            var result = new ContentResult() {
                Content = content,
                ContentType = "application/json",
                StatusCode = (int) HttpStatusCode.InternalServerError,
            };
            context.ExceptionHandled = true;
            return result.ExecuteResultAsync(context);
        }
    }
}
