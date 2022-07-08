using System.Data;
using Microsoft.EntityFrameworkCore;

namespace Stl.Fusion.EntityFramework;

public class GlobalIsolationLevelSelector
{
    private readonly Func<CommandContext, IsolationLevel>? _selector;

    public GlobalIsolationLevelSelector(Func<CommandContext, IsolationLevel>? selector)
        => _selector = selector;

    public virtual IsolationLevel GetCommandIsolationLevel(CommandContext commandContext)
        => _selector?.Invoke(commandContext) ?? IsolationLevel.Unspecified;
}

public class DbIsolationLevelSelector<TDbContext>
    where TDbContext : DbContext
{
    private readonly Func<CommandContext, IsolationLevel>? _selector;

    public DbIsolationLevelSelector(Func<CommandContext, IsolationLevel>? selector)
        => _selector = selector;

    public virtual IsolationLevel GetCommandIsolationLevel(CommandContext commandContext)
        => _selector?.Invoke(commandContext) ?? IsolationLevel.Unspecified;
}
