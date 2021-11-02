using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Server;
using Templates.TodoApp.Abstractions;

namespace Templates.TodoApp.Host.Controllers;

[Route("api/[controller]/[action]")]
[ApiController, JsonifyErrors]
public class TodoController : ControllerBase, ITodoService
{
    private readonly ITodoService _todos;
    private readonly ISessionResolver _sessionResolver;

    public TodoController(ITodoService todos, ISessionResolver sessionResolver)
    {
        _todos = todos;
        _sessionResolver = sessionResolver;
    }

    // Commands

    [HttpPost]
    public Task<Todo> AddOrUpdate([FromBody] AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        => _todos.AddOrUpdate(command.UseDefaultSession(_sessionResolver), cancellationToken);

    [HttpPost]
    public Task Remove([FromBody] RemoveTodoCommand command, CancellationToken cancellationToken = default)
        => _todos.Remove(command.UseDefaultSession(_sessionResolver), cancellationToken);

    // Queries

    [HttpGet, Publish]
    public Task<Todo?> Get(Session session, string id, CancellationToken cancellationToken = default)
        => _todos.Get(session, id, cancellationToken);

    [HttpGet, Publish]
    public Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
        => _todos.List(session, pageRef, cancellationToken);

    [HttpGet, Publish]
    public Task<TodoSummary> GetSummary(Session session, CancellationToken cancellationToken = default)
        => _todos.GetSummary(session, cancellationToken);
}
