namespace Stl.Tests.Serialization;

public class ExceptionInfoTest : TestBase
{
    public class WeirdException : Exception
    {
        public WeirdException() : base("") { }
    }

    public ExceptionInfoTest(ITestOutputHelper @out) : base(@out) { }

    [Fact]
    public void BasicTest()
    {
        var i = new ExceptionInfo(new Exception("1"));
        i.Message.Should().Be("1");
        i.ToException().Should().BeOfType<Exception>()
            .Which.Message.Should().Be("1");

        // ReSharper disable once NotResolvedInText
        i = new ExceptionInfo(new ArgumentNullException("none", "2"));
        i.Message.Should().Be("2 (Parameter 'none')");
        i.ToException().Should().BeOfType<ArgumentNullException>()
            .Which.Message.Should().Be("2 (Parameter 'none')");

        // ReSharper disable once NotResolvedInText
        i = new ExceptionInfo(new WeirdException());
        i.Message.Should().Be("");
        ((RemoteException) i.ToException()!).ExceptionInfo.Should().Be(i);
    }
}
