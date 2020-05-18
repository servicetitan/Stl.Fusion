using System;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators.Emitters;

namespace Stl.Fusion.Interception.Internal
{
    public class ComputedClassProxyGenerator : Castle.DynamicProxy.Generators.ClassProxyGenerator
    {
        public ComputedClassProxyGenerator(ModuleScope scope, Type targetType) 
            : base(scope, targetType) { }

        protected override void CreateFields(ClassEmitter emitter)
        {
            CreateOptionsField(emitter);
            CreateSelectorField(emitter);
            CreateInterceptorsField(emitter);
        }

        protected new void CreateInterceptorsField(ClassEmitter emitter)
        {
            emitter.CreateField("__interceptors", typeof (ComputedInterceptor[]));
        }
    }
}
