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

        jsonifyErrorsFixture.OnExceptionAsync(exceptionContext);

        exceptionContextFixture.VerifyLogError(expectedMessage);
    }

    [Theory]
    [InlineData(true)]
    [InlineData(false)]
    public void OnException_Should_RewriteErrorsIfSet(bool rewriteErrors)
    {
        var exceptionContextFixture = new ExceptionContextFixture();
        var exceptionContext = exceptionContextFixture.Create();
        var jsonifyErrorsFixture = new JsonifyErrorsAttributeFixture().WithRewriteErrors(rewriteErrors);

        jsonifyErrorsFixture.OnExceptionAsync(exceptionContext);

        exceptionContextFixture.VerifyErrorRewrite(rewriteErrors);
    }
}
