using System.Runtime.Serialization;
using Stl.Fusion.Blazor;
using Stl.Fusion.Extensions;

namespace Templates.TodoApp.Abstractions;

[DataContract]
[ParameterComparer(typeof(ByValueParameterComparer))]
public sealed record Todo(
    [property: DataMember] string Id,
    [property: DataMember] string Title,
    [property: DataMember] bool IsDone = false)
{
    public Todo() : this("", "") { }
}

[DataContract]
public sealed record TodoSummary(
    [property: DataMember] int Count,
    [property: DataMember] int DoneCount)
{
    public TodoSummary() : this(0, 0) { }
}

[DataContract]
public sealed record AddOrUpdateTodoCommand(
    [property: DataMember] Session Session,
    [property: DataMember] Todo Item
    ) : ISessionCommand<Todo>;

[DataContract]
public sealed record RemoveTodoCommand(
    [property: DataMember] Session Session,
    [property: DataMember] string Id
    ) : ISessionCommand<Unit>;

public interface ITodoService : IComputeService
{
    // Commands
    [CommandHandler]
    Task<Todo> AddOrUpdate(AddOrUpdateTodoCommand command, CancellationToken cancellationToken = default);
    [CommandHandler]
    Task Remove(RemoveTodoCommand command, CancellationToken cancellationToken = default);

    // Queries
    [ComputeMethod]
    Task<Todo?> Get(Session session, string id, CancellationToken cancellationToken = default);
    [ComputeMethod]
    Task<Todo[]> List(Session session, PageRef<string> pageRef, CancellationToken cancellationToken = default);
    [ComputeMethod(InvalidationDelay = 1)]
    Task<TodoSummary> GetSummary(Session session, CancellationToken cancellationToken = default);
}
