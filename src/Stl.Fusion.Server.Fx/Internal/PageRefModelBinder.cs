using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Web.Http.Controllers;
using System.Web.Http.ModelBinding;
//using System.Web.Mvc;
using Stl.Fusion.Extensions;

namespace Stl.Fusion.Server.Internal
{
    public class PageRefModelBinder : IModelBinder
    {
        private static readonly MethodInfo ParseMethod = typeof(PageRef).GetMethod(nameof(PageRef.Parse))!;

        //public object BindModel(ControllerContext controllerContext, ModelBindingContext bindingContext)
        //{
        //    if (bindingContext == null)
        //        throw new ArgumentNullException(nameof(bindingContext));

        //    //try {
        //    //    var sValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue ?? "";
        //    //    var result = ParseMethod
        //    //        .MakeGenericMethod(bindingContext.ModelType.GetGenericArguments()[0])
        //    //        .Invoke(null, new object[] { sValue });
        //    //    bindingContext.Result = ModelBindingResult.Success(result);
        //    //}
        //    //catch (Exception) {
        //    //    bindingContext.Result = ModelBindingResult.Failed();
        //    //}
        //    //return Task.CompletedTask;

        //    return null;
        //}

        public bool BindModel(HttpActionContext actionContext, ModelBindingContext bindingContext)
        {
            return true;
        }
    }
}
