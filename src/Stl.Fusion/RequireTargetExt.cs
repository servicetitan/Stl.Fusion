using Stl.Requirements;

namespace Stl.Fusion;

public static class RequireTargetExt
{
    // Normal overloads

    public static T RequireResult<T>(this T? target, Requirement<T>? requirement = null)
        where T : IRequireTarget
    {
        try {
            requirement ??= NotNullOrDefaultRequirement<T>.Default;
            return requirement.Require(target);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }

    public static async Task<T> RequireResult<T>(this Task<T?> targetSource, Requirement<T>? requirement = null)
        where T : IRequireTarget
    {
        try {
            var target = await targetSource.ConfigureAwait(false);
            requirement ??= NotNullOrDefaultRequirement<T>.Default;
            return requirement.Require(target);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }

    public static async ValueTask<T> RequireResult<T>(this ValueTask<T?> targetSource, Requirement<T>? requirement = null)
        where T : IRequireTarget
    {
        try {
            var target = await targetSource.ConfigureAwait(false);
            requirement ??= NotNullOrDefaultRequirement<T>.Default;
            return requirement.Require(target);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }

    // Overloads accepting requirement builder

    public static T RequireResult<T>(this T? target, Func<Requirement<T>> requirementBuilder)
        where T : IRequireTarget
    {
        try {
            var requirement = requirementBuilder.Invoke();
            return requirement.Require(target);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }

    public static async Task<T> RequireResult<T>(this Task<T?> targetSource, Func<Requirement<T>> requirementBuilder)
        where T : IRequireTarget
    {
        try {
            var target = await targetSource.ConfigureAwait(false);
            var requirement = requirementBuilder.Invoke();
            return requirement.Require(target);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }

    public static async ValueTask<T> RequireResult<T>(this ValueTask<T?> targetSource, Func<Requirement<T>> requirementBuilder)
        where T : IRequireTarget
    {
        try {
            var target = await targetSource.ConfigureAwait(false);
            var requirement = requirementBuilder.Invoke();
            return requirement.Require(target);
        }
        catch (Exception e) when (e is not OperationCanceledException) {
            throw new ResultException(e.Message, e);
        }
    }
}
