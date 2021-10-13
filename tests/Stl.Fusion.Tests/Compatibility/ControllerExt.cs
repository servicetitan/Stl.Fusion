#if NETFRAMEWORK

using System.Threading;
using System.Web.Http;

namespace Stl.Fusion.Tests
{
    internal static class ControllerExt
    {
        public static CancellationToken RequestAborted(this ApiController controller)
        {
            return controller.ActionContext.RequestAborted();
        }
    }
}

#else

using System.Threading;
using Microsoft.AspNetCore.Mvc;

namespace Stl.Fusion.Tests
{
    internal static class ControllerExt
    {
        public static CancellationToken RequestAborted(this ControllerBase controller)
        {
            return controller.HttpContext.RequestAborted;
        }
    }
}

#endif
