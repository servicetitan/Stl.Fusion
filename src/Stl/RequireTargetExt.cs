using System.Diagnostics.CodeAnalysis;
using Stl.Requirements;

namespace Stl;

public static class RequireTargetExt
{
    // Normal overloads

    public static T Require<T>(this T? target, Requirement<T>? requirement = null)
        where T : IRequireTarget
    {
        requirement ??= NotNullOrDefaultRequirement<T>.Default; 
        return requirement.Require(target);
    }

    public static async Task<T> Require<T>(this Task<T?> targetSource, Requirement<T>? requirement = null)
        where T : IRequireTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        requirement ??= NotNullOrDefaultRequirement<T>.Default;
        return requirement.Require(target);
    }

    public static async ValueTask<T> Require<T>(this ValueTask<T?> targetSource, Requirement<T>? requirement = null)
        where T : IRequireTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        requirement ??= NotNullOrDefaultRequirement<T>.Default; 
        return requirement.Require(target);
    }

    // Overloads accepting requirement builder

    public static T Require<T>(this T? target, Func<Requirement<T>> requirementBuilder)
        where T : IRequireTarget 
        => requirementBuilder.Invoke().Require(target);

    public static async Task<T> Require<T>(this Task<T?> targetSource, Func<Requirement<T>> requirementBuilder)
        where T : IRequireTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        return requirementBuilder.Invoke().Require(target);
    }

    public static async ValueTask<T> Require<T>(this ValueTask<T?> targetSource, Func<Requirement<T>> requirementBuilder)
        where T : IRequireTarget
    {
        var target = await targetSource.ConfigureAwait(false);
        return requirementBuilder.Invoke().Require(target);
    }
}
