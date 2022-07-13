using Stl.Fusion.Requirements;
using Stl.Requirements;

namespace Stl.Fusion;

public static class RequirementExt
{
    public static UseServiceExceptionRequirement<T> UseServiceException<T>(this Requirement<T> requirement)
        => ReferenceEquals(requirement, NotNullOrDefaultRequirement<T>.Default)
            ? UseServiceExceptionRequirement<T>.Default
            : new UseServiceExceptionRequirement<T>(requirement);
}
