namespace Stl.Requirements;

public record ServiceExceptionWrapper<T>(Requirement<T> BaseRequirement) : Requirement<T>
{
    public static ServiceExceptionWrapper<T> Default { get; } =
        new(MustExistRequirement<T>.Default);

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
