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
    public Task OnExceptionAsync(
#if NETFRAMEWORK
        HttpActionExecutedContext
#else
        ExceptionContext
#endif
            context)
    {
#if NETFRAMEWORK
        return Create().OnExceptionAsync(context, default);
#else
        return Create().OnExceptionAsync(context);
#endif
    }
}
