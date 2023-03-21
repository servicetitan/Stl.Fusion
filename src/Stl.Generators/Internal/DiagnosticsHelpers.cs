namespace Stl.Generators.Internal;

public static class DiagnosticsHelpers
{
    public static readonly bool IsDebugOutputEnabled =
#if DEBUG
        false;
#else
        false;
#endif

    private static readonly DiagnosticDescriptor DebugDescriptor = new(
        id: "STLGDEBUG",
        title: "Debug warning",
        messageFormat: "Debug warning: {0}",
        category: nameof(ProxyGenerator),
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true);

    private static readonly DiagnosticDescriptor GenerateProxyTypeProcessedDescriptor = new(
        id: "STLG0001",
        title: "[GenerateProxy]: type processed.",
        messageFormat: "[GenerateProxy]: type '{0}' is processed.",
        category: nameof(ProxyGenerator),
        DiagnosticSeverity.Info,
        isEnabledByDefault: true);

    // Diagnostics

    public static readonly Action<string>? WriteDebug = IsDebugOutputEnabled
        ? WriteDebugImpl
        : null;

    public static void ReportDebug(this SourceProductionContext context, string text, Location? location = null)
    {
        if (IsDebugOutputEnabled)
            context.ReportDiagnostic(DebugWarning(text, location));
    }

    public static void ReportDebug(this SourceProductionContext context, Exception error)
    {
        if (IsDebugOutputEnabled)
            context.ReportDiagnostic(DebugWarning(error));
    }

    public static Diagnostic DebugWarning(string text, Location? location = null)
        => Diagnostic.Create(DebugDescriptor, location ?? Location.None, text);

    public static Diagnostic DebugWarning(Exception error)
    {
        var text = (error.ToString() ?? "")
            .Replace("\r\n", " | ")
            .Replace("\n", " | ");
        return DebugWarning(text);
    }

    public static Diagnostic GenerateProxyTypeProcessedInfo(TypeDeclarationSyntax typeDef)
        => Diagnostic.Create(GenerateProxyTypeProcessedDescriptor, typeDef.GetLocation(), typeDef.Identifier.Text);

    // Private methods

    private static void WriteDebugImpl(string message)
    {
        for (var i = 0; i < 5; i++) {
            try {
                File.AppendAllText("C:/Temp/Stl.Generators.txt", message + Environment.NewLine, Encoding.UTF8);
                return;
            }
            catch (IOException) {
                // Intended
            }
        }
    }
}
