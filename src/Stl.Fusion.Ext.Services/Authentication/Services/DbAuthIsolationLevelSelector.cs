using System.Data;
using Microsoft.EntityFrameworkCore;
using Stl.Fusion.EntityFramework;

namespace Stl.Fusion.Authentication.Services;

public class DbAuthIsolationLevelSelector<TDbContext>() : DbIsolationLevelSelector<TDbContext>(null)
    where TDbContext : DbContext
{
    public override IsolationLevel GetCommandIsolationLevel(CommandContext commandContext)
    {
        var command = commandContext.UntypedCommand;
        switch (command) {
        case AuthBackend_SignIn:
        case Auth_SignOut:
        case AuthBackend_SetupSession:
        case Auth_SetSessionOptions:
            return IsolationLevel.ReadCommitted;
        }
        return IsolationLevel.Unspecified;
    }
}
