using Stl.Requirements;

namespace Stl.Fusion.Requirements;

public record UseServiceExceptionRequirement<T>(Requirement<T> BaseRequirement) : Requirement<T>
{
    public static UseServiceExceptionRequirement<T> Default { get; } = new(NotNullOrDefaultRequirement<T>.Default);

    public override bool IsSatisfied(T? value)
        => BaseRequirement.IsSatisfied(value);

    public override T Check(T? value)
    {
        try {
            return BaseRequirement.Check(value);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ServiceException(e.Message, e);
        }
    }
}
