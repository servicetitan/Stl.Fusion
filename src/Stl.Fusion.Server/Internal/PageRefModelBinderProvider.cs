using System;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Stl.Fusion.Extensions;

namespace Stl.Fusion.Server.Internal
{
    public class PageRefModelBinderProvider  : IModelBinderProvider
    {
        public IModelBinder? GetBinder(ModelBinderProviderContext context)
        {
            if (context == null)
                throw new ArgumentNullException(nameof(context));

            var modelType = context.Metadata.ModelType;
            if (modelType.IsConstructedGenericType && modelType.GetGenericTypeDefinition() == typeof(PageRef<>))
                return new PageRefModelBinder();

            return null;
        }
    }
}
