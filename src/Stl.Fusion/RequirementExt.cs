using Stl.Fusion.Requirements;
using Stl.Requirements;

namespace Stl.Fusion;

public static class RequirementExt
{
    public static ResultExceptionRequirement<T> UseResultException<T>(this Requirement<T> requirement)
        => ReferenceEquals(requirement, NotNullOrDefaultRequirement<T>.Default)
            ? ResultExceptionRequirement<T>.Default
            : new ResultExceptionRequirement<T>(requirement);
}
