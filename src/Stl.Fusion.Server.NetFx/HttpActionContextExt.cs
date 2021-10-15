using System.Web.Http.Controllers;
using Stl.Internal;

namespace Stl.Fusion.Server;

public static class HttpActionContextExt
{
    public const string ItemsKey = "@@items";

    public static IDictionary<object, object> GetItems(this HttpActionContext httpContext)
    {
        // https://stackoverflow.com/questions/18690500/how-to-store-global-per-request-data-in-a-net-web-api-project

#pragma warning disable 618
        var requestProperties = httpContext.Request.Properties;
#pragma warning restore 618
        IDictionary<object, object>? items = null;
        if (requestProperties.TryGetValue(ItemsKey, out var obj)) {
            items = obj as IDictionary<object,object>;
            if (items == null && obj != null)
                throw Errors.InternalError($"'{ItemsKey}' key is used by something else.");
        }
        if (items == null) {
            items = new Dictionary<object, object>();
            requestProperties[ItemsKey] = items;
        }
        return items;
    }
}
