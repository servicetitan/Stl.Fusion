#if !NETCOREAPP

using System.Net.Http;
using System.Threading;
using System.Web.Http.Controllers;

namespace Stl.Fusion.Tests;

internal static class ActionContextExt
{
    public static CancellationToken RequestAborted(this HttpActionContext actionContext)
    {
        return actionContext.Request.RequestAborted();
    }

    public static CancellationToken RequestAborted(this HttpRequestMessage requestMessage)
    {
        return requestMessage.GetOwinContext().Request.CallCancelled;
    }
}

#endif
