namespace Stl.Fusion.Tests.Server;

public class JsonifyErrorsAttributeTests
{
    [Fact]
    public void OnException_Should_Log()
    {
        var expectedMessage = "A";
        var exceptionContextFixture = new ExceptionContextFixture().WithException(expectedMessage);
        var exceptionContext = exceptionContextFixture.Create();
        var jsonifyErrorsFixture = new JsonifyErrorsAttributeFixture();

        _ = jsonifyErrorsFixture.OnExceptionAsync(exceptionContext);

        exceptionContextFixture.VerifyLogError(expectedMessage);
    }

    [Fact]
    public void OnException_Should_RewriteErrorsIfSet()
    {
        var exceptionContextFixture = new ExceptionContextFixture();
        var exceptionContext = exceptionContextFixture.Create();
        var jsonifyErrorsFixture = new JsonifyErrorsAttributeFixture();

        _ = jsonifyErrorsFixture.OnExceptionAsync(exceptionContext);
    }
}
