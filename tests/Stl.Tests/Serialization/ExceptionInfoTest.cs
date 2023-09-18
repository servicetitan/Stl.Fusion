namespace Stl.Tests.Serialization;

public class ExceptionInfoTest(ITestOutputHelper @out) : TestBase(@out)
{
#pragma warning disable RCS1194
    public class WeirdException() : Exception("")
#pragma warning restore RCS1194
    { }

    [Fact]
    public void BasicTest()
    {
        var e = new Exception("1");
        var i = e.ToExceptionInfo();
        i.Message.Should().Be("1");
        i.ToException().Should().BeOfType<Exception>()
            .Which.Message.Should().Be("1");

        // ReSharper disable once NotResolvedInText
#pragma warning disable MA0015
        e = new ArgumentNullException("none", "2");
#pragma warning restore MA0015
        i = e.ToExceptionInfo();
        i.Message.Should().Be(e.Message);
        i.ToException().Should().BeOfType<ArgumentNullException>()
            .Which.Message.Should().Be(e.Message);

        // ReSharper disable once NotResolvedInText
        i = new WeirdException().ToExceptionInfo();
        i.Message.Should().Be("");
        ((RemoteException) i.ToException()!).ExceptionInfo.Should().Be(i);
    }

    [Fact]
    public void LegacyExceptionInfoTest()
    {
        var t = new TypeEvolutionTester<OldExceptionInfo, ExceptionInfo>(
            (li, i) => Assert.True(li.TypeRef == i.TypeRef && Equals(li.Message, i.Message)));
        t.CheckAllSerializers(new OldExceptionInfo(typeof(Exception), "1"));
    }
}
