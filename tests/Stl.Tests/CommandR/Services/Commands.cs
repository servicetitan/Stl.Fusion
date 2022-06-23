namespace Stl.Tests.CommandR.Services;

public record LogCommand : ICommand<Unit>
{
    public string Message { get; set; } = "";
}

public record DivCommand : ICommand<double>
{
    public double Divisible { get; set; }
    public double Divisor { get; set; }
}

public record RecSumCommand : ICommand<double>
{
    public static AsyncLocal<object> Tag { get; } = new();

    public double[] Arguments { get; set; } = Array.Empty<double>();
}

public record RecAddUsersCommand : ICommand<Unit>
{
    public User[] Users { get; set; } = Array.Empty<User>();
}
