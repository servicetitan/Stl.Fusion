using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Stl.Fusion.Authentication;
using Stl.Fusion.Extensions;
using Stl.Fusion.Server;
using Templates.Blazor2.Abstractions;

namespace Templates.Blazor2.Host.Controllers
{
    [Route("api/[controller]")]
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

        [HttpPost("addOrUpdate")]
        public Task<Todo> AddOrUpdateAsync([FromBody] AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(_sessionResolver);
            return _todos.AddOrUpdateAsync(command, cancellationToken);
        }

        [HttpPost("remove")]
        public Task RemoveAsync([FromBody] RemoveTodoCommand command, CancellationToken cancellationToken = default)
        {
            command.UseDefaultSession(_sessionResolver);
            return _todos.RemoveAsync(command, cancellationToken);
        }

        // Queries

        [HttpGet("find"), Publish]
        public Task<Todo?> FindAsync(Session? session, string id, CancellationToken cancellationToken = default)
        {
            session ??= _sessionResolver.Session;
            return _todos.FindAsync(session, id, cancellationToken);
        }

        [HttpGet("list"), Publish]
        public Task<Todo[]> ListAsync(Session? session, PageRef<string> pageRef, CancellationToken cancellationToken = default)
        {
            session ??= _sessionResolver.Session;
            return _todos.ListAsync(session, pageRef, cancellationToken);
        }
    }
}
