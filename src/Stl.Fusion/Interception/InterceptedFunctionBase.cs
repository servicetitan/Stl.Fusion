using Stl.Fusion.Interception.Internal;

namespace Stl.Fusion.Interception
{
    public abstract class InterceptedFunctionBase<TOut> : FunctionBase<InterceptedInput, TOut>
    {
        public InterceptedMethod Method { get; }

        protected InterceptedFunctionBase(InterceptedMethod method)
            => Method = method;

        public override string ToString()
        {
            var mi = Method.MethodInfo;
            return $"Intercepted:{mi.DeclaringType!.Name}.{mi.Name}";
        }
    }
}
