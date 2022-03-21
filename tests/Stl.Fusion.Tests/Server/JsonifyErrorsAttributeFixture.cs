#if NETFRAMEWORK
using System.Web.Http.Filters;
#else
using Microsoft.AspNetCore.Mvc.Filters;
#endif

using AutoFixture;
using Stl.Fusion.Server;

namespace Stl.Fusion.Tests.Server;

public class JsonifyErrorsAttributeFixture : BaseFixture<JsonifyErrorsAttribute>
{
    private bool _rewriteErrors = true;

    public JsonifyErrorsAttributeFixture()
    {
        Fixture.Register(() => new JsonifyErrorsAttribute {RewriteErrors = _rewriteErrors});
    }

    public JsonifyErrorsAttributeFixture WithRewriteErrors(bool value)
    {
        _rewriteErrors = value;
        return this;
    }

    public Task OnExceptionAsync(
#if NETFRAMEWORK
        HttpActionExecutedContext
#else
        ExceptionContext
#endif
            context)
    {
#if NETFRAMEWORK
        return Create().OnExceptionAsync(context, CancellationToken.None);
#else
        return Create().OnExceptionAsync(context);
#endif
    }
}