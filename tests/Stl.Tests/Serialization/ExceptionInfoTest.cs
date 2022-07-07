namespace Stl.Tests.Serialization;

public class ExceptionInfoTest : TestBase
{
#pragma warning disable RCS1194
    public class WeirdException : Exception
#pragma warning restore RCS1194
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
#pragma warning disable MA0015
        i = new ExceptionInfo(new ArgumentNullException("none", "2"));
#pragma warning restore MA0015
        i.Message.Should().Be("2 (Parameter 'none')");
        i.ToException().Should().BeOfType<ArgumentNullException>()
            .Which.Message.Should().Be("2 (Parameter 'none')");

        // ReSharper disable once NotResolvedInText
        i = new ExceptionInfo(new WeirdException());
        i.Message.Should().Be("");
        ((RemoteException) i.ToException()!).ExceptionInfo.Should().Be(i);
    }
}
