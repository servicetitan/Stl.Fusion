namespace Stl.Fusion.Interception;

public interface IComputeFunction
{
    ComputeMethodDef MethodDef { get; }
    ComputedOptions ComputedOptions { get; }
}

public abstract class ComputeFunctionBase<T> : FunctionBase<T>, IComputeFunction
{
    public ComputeMethodDef MethodDef { get; }
    public ComputedOptions ComputedOptions { get; }

    protected ComputeFunctionBase(ComputeMethodDef methodDef, IServiceProvider services)
        : base(services)
    {
        MethodDef = methodDef;
        ComputedOptions = methodDef.ComputedOptions;
    }

    public override string ToString()
        => MethodDef.FullName;
}
