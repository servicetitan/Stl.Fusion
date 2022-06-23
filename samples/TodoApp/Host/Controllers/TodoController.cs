using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Extensions;
using Stl.Fusion.Server;
using Templates.TodoApp.Abstractions;

namespace Templates.TodoApp.Host.Controllers;

[Route("api/[controller]/[action]")]
[ApiController, JsonifyErrors, UseDefaultSession]
public class TodoController : ControllerBase, ITodoService
{
    private readonly ITodoService _todos;
    private readonly ICommander _commander;

    public TodoController(ITodoService todos, ICommander commander)
    {
        _todos = todos;
        _commander = commander;
    }

    // Commands

    [HttpPost]
    public Task<Todo> AddOrUpdate([FromBody] AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        => _commander.Call(command, cancellationToken);

    [HttpPost]
    public Task Remove([FromBody] RemoveTodoCommand command, CancellationToken cancellationToken = default)
        => _commander.Call(command, cancellationToken);

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
