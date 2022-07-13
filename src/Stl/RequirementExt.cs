using Stl.Requirements;

namespace Stl;

public static class RequirementExt
{
    public static Requirement<T> WithServiceException<T>(this Requirement<T> requirement)
    {
        if (requirement is ServiceExceptionWrapper<T>)
            return requirement;
        return ReferenceEquals(requirement, NotNullOrDefaultRequirement<T>.Default)
            ? ServiceExceptionWrapper<T>.Default
            : new ServiceExceptionWrapper<T>(requirement);
    }
}
