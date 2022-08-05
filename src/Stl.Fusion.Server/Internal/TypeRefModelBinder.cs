using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Stl.Fusion.Server.Internal;

public class TypeRefModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        try {
            var sValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue ?? "";
            var result = new TypeRef(sValue);
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception) {
            bindingContext.Result = ModelBindingResult.Failed();
        }
        return Task.CompletedTask;
    }
}
