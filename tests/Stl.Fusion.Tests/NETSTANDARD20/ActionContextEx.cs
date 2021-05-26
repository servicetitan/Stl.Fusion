#if !NETCOREAPP

using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Web.Http.Controllers;

namespace Stl.Fusion.Tests
{
    internal static class ActionContextEx
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
}

#endif