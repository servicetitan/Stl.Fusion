using System;
using System.Threading.Tasks;
using Stl.Async;

namespace Stl.Fusion.Interception
{
    public abstract class ComputeFunctionBase<TOut> : FunctionBase<ComputeMethodInput, TOut>
    {
        public ComputeMethodDef Method { get; }
        protected ComputedOptions Options { get; }

        protected ComputeFunctionBase(ComputeMethodDef method, IServiceProvider services)
            : base(services)
        {
            Method = method;
            Options = method.Options;
        }

        public override string ToString()
        {
            var mi = Method.MethodInfo;
            return $"Intercepted:{mi.DeclaringType!.Name}.{mi.Name}";
        }

        // Protected methods

        protected static void SetReturnValue(ComputeMethodInput input, Result<TOut> output)
        {
            if (input.Method.ReturnsValueTask)
                input.Invocation.ReturnValue =
                    // ReSharper disable once HeapView.BoxingAllocation
                    output.IsValue(out var v)
                        ? ValueTaskExt.FromResult(v)
                        : ValueTaskExt.FromException<TOut>(output.Error!);
            else
                input.Invocation.ReturnValue =
                    output.IsValue(out var v)
                        ? Task.FromResult(v)
                        : Task.FromException<TOut>(output.Error!);
        }
    }
}
