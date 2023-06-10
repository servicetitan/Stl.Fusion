using System.Data;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.Authentication.Commands;
using Stl.Fusion.EntityFramework;

namespace Stl.Fusion.Authentication.Services;

public class DbAuthIsolationLevelSelector<TDbContext> : DbIsolationLevelSelector<TDbContext>
    where TDbContext : DbContext
{
    public DbAuthIsolationLevelSelector() : base(null) { }

    public override IsolationLevel GetCommandIsolationLevel(CommandContext commandContext)
    {
        var command = commandContext.UntypedCommand;
        switch (command) {
        case SignInCommand:
        case SignOutCommand:
        case SetupSessionCommand:
        case SetSessionOptionsCommand:
            return IsolationLevel.ReadCommitted;
        }
        return IsolationLevel.Unspecified;
    }
}
