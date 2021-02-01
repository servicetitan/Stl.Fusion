using System;
using System.Reflection;
using Microsoft.AspNetCore.Mvc.Controllers;

namespace Stl.Fusion.Server.Internal
{
    public class ControllerFilter : ControllerFeatureProvider
    {
        private Func<TypeInfo, bool> Filter { get; }

        public ControllerFilter(Func<TypeInfo, bool> filter)
            => Filter = filter;

        protected override bool IsController(TypeInfo typeInfo) {
            if (!Filter.Invoke(typeInfo))
                return false;
            return base.IsController(typeInfo);
        }
    }
}
