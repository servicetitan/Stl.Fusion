using Microsoft.AspNetCore.Mvc.ModelBinding;

namespace Stl.Fusion.Server.Internal;

public class RangeModelBinder : IModelBinder
{
    public Task BindModelAsync(ModelBindingContext bindingContext)
    {
        if (bindingContext == null)
            throw new ArgumentNullException(nameof(bindingContext));

        try {
            var sValue = bindingContext.ValueProvider.GetValue(bindingContext.ModelName).FirstValue ?? "";
#pragma warning disable IL2026
            var result = typeof(Range<>)
                .MakeGenericType(bindingContext.ModelType.GetGenericArguments()[0])
                .GetMethod(nameof(Range<long>.Parse))!
                .Invoke(null, new object[] { sValue });
#pragma warning restore IL2026
            bindingContext.Result = ModelBindingResult.Success(result);
        }
        catch (Exception) {
            bindingContext.Result = ModelBindingResult.Failed();
        }
        return Task.CompletedTask;
    }
}
