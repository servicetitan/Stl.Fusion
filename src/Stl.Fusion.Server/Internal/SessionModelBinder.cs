using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.Extensions.DependencyInjection;
using Stl.Fusion.Authentication;

namespace Stl.Fusion.Server.Internal
{
    public class SessionModelBinder : IModelBinder
    {
        public Task BindModelAsync(ModelBindingContext bindingContext)
        {
            if (bindingContext == null)
                throw new ArgumentNullException(nameof(bindingContext));

            Task UseDefaultSession()
            {
                try {
                    var sessionResolver = bindingContext.HttpContext.RequestServices.GetRequiredService<ISessionResolver>();
                    bindingContext.Result = ModelBindingResult.Success(sessionResolver.Session);
                    return Task.CompletedTask;
                }
                catch (Exception) {
                    bindingContext.Result = ModelBindingResult.Failed();
                    return Task.CompletedTask;
                }
            }

            try {
                var sValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue ?? "";
                if (sValue == "")
                    return UseDefaultSession();
                bindingContext.Result = ModelBindingResult.Success(new Session(sValue));
                return Task.CompletedTask;
            }
            catch (Exception) {
                return UseDefaultSession();
            }
        }
    }
}
