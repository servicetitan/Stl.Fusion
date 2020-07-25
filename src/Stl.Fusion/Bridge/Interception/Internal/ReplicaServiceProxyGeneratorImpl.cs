using System;
using Castle.DynamicProxy;
using Castle.DynamicProxy.Generators.Emitters;

namespace Stl.Fusion.Bridge.Interception.Internal
{
    public class ReplicaServiceProxyGeneratorImpl : Castle.DynamicProxy.Generators.InterfaceProxyWithTargetInterfaceGenerator
    {
        public ReplicaServiceProxyGeneratorImpl(ModuleScope scope, Type @interface)
            : base(scope, @interface) { }

        protected override void CreateFields(ClassEmitter emitter)
        {
            CreateOptionsField(emitter);
            CreateSelectorField(emitter);
            CreateInterceptorsField(emitter);
        }

        protected new void CreateInterceptorsField(ClassEmitter emitter)
        {
            emitter.CreateField("__interceptors", typeof (ReplicaServiceInterceptor[]));
        }
    }
}
