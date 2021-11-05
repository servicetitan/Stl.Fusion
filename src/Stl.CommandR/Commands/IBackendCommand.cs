namespace Stl.CommandR.Commands;

/// <summary>
/// A tagging interface for commands that can't be initiated by the client.
/// </summary>
/// <remarks>
/// As of v2.0+, Fusion doesn't perform any filtering for such commands,
/// because normally there are no controller endpoints accepting them.
/// But if you want some extra security, you consider adding your own
/// MVC action filter preventing such commands from being executed
/// via any of your public endpoints.
/// </remarks>
public interface IBackendCommand : ICommand
{ }
