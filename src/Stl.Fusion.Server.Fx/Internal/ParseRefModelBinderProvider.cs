//using System;
//using System.Web.Mvc;
//using Stl.Fusion.Extensions;

//namespace Stl.Fusion.Server.Internal
//{
//    public class ParseRefModelBinderProvider  : IModelBinderProvider
//    {
//        public IModelBinder GetBinder(Type modelType)
//        {
//            if (modelType.IsConstructedGenericType && modelType.GetGenericTypeDefinition() == typeof(PageRef<>))
//                return new PageRefModelBinder();

//            return null;
//        }
//    }
//}
