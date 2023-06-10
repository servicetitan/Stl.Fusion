using Stl.Fusion.Authentication;
using Stl.Generators;

namespace Stl.Fusion.Tests;

public class SessionProviderTest
{
    [Fact]
    public void BasicTest()
    {
        var services = new ServiceCollection()
            .AddFusion().AddAuthentication()
            .Services
            .BuildServiceProvider();

        var session = new Session(RandomSymbolGenerator.Default.Next());

        // Root
        var c = (IServiceProvider) services;
        c.IsScoped().Should().BeFalse();

        var sp = c.GetRequiredService<ISessionResolver>();
        c.GetRequiredService<ISessionResolver>().Should().Be(sp);
        sp.HasSession.Should().BeFalse();
        sp.SessionTask.IsCompleted.Should().BeFalse();
        Assert.Throws<InvalidOperationException>(() => c.GetRequiredService<Session>());
        Assert.Throws<InvalidOperationException>(() => sp.Session = session);
        sp.HasSession.Should().BeFalse();
        sp.SessionTask.IsCompleted.Should().BeFalse();

        // Scoped
        using var scope = c.CreateScope();
        c = scope.ServiceProvider;
        c.IsScoped().Should().BeTrue();

        sp = c.GetRequiredService<ISessionResolver>();
        c.GetRequiredService<ISessionResolver>().Should().Be(sp);
        sp.HasSession.Should().BeFalse();
        sp.SessionTask.IsCompleted.Should().BeFalse();
        Assert.Throws<InvalidOperationException>(() => c.GetRequiredService<Session>());
        sp.Session = session;
        sp.HasSession.Should().BeTrue();
        sp.SessionTask.IsCompleted.Should().BeTrue();
        c.GetRequiredService<Session>().Should().Be(session);
    }
}
