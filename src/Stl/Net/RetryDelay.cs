namespace Stl.Net;

public readonly record struct RetryDelay(
    Task DelayTask,
    Moment EndsAt)
{
    public override string ToString()
        => $"{nameof(RetryDelay)}(EndsAt = {EndsAt.ToDateTime():T})";

    public static implicit operator RetryDelay((Task DelayTask, Moment EndsAt) source)
        => new(source.DelayTask, source.EndsAt);
}
