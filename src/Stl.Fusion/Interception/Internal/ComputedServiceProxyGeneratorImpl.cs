using System;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators.Emitters;

namespace Stl.Fusion.Interception.Internal
{
    public class ComputedServiceProxyGeneratorImpl : Castle.DynamicProxy.Generators.ClassProxyGenerator
    {
        public ComputedServiceProxyGeneratorImpl(ModuleScope scope, Type targetType) 
            : base(scope, targetType) { }

        protected override void CreateFields(ClassEmitter emitter)
        {
            CreateOptionsField(emitter);
            CreateSelectorField(emitter);
            CreateInterceptorsField(emitter);
        }

        protected new void CreateInterceptorsField(ClassEmitter emitter)
        {
            emitter.CreateField("__interceptors", typeof (ComputedServiceInterceptor[]));
        }
    }
}
