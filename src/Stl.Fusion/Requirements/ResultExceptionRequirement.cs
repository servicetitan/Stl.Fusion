using Stl.Requirements;

namespace Stl.Fusion.Requirements;

public record ResultExceptionRequirement<T>(Requirement<T> BaseRequirement) : Requirement<T>
{
    public static ResultExceptionRequirement<T> Default { get; } = new(NotNullOrDefaultRequirement<T>.Default);

    public override bool IsSatisfied(T? value) 
        => BaseRequirement.IsSatisfied(value);

    public override T Check(T? value)
    {
        try {
            return BaseRequirement.Check(value);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }
}
