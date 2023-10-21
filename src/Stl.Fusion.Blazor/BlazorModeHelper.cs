using System.Web;
using Microsoft.AspNetCore.Components;
using Stl.OS;

namespace Stl.Fusion.Blazor;

public class BlazorModeHelper(NavigationManager navigator)
{
    public static readonly bool IsBlazorServer = OSInfo.Kind != OSKind.WebAssembly;

    protected NavigationManager Navigator { get; } = navigator;

    public virtual void ChangeMode(bool isBlazorServer)
    {
        if (IsBlazorServer == isBlazorServer)
            return;

        var switchUrl = GetModeChangeUrl(isBlazorServer, Navigator.Uri);
        Navigator.NavigateTo(switchUrl, true);
    }

    public virtual string GetModeChangeUrl(bool isBlazorServer, string? redirectTo = null)
    {
        redirectTo ??= "/";
        return $"/fusion/blazorMode/{(isBlazorServer ? "1" : "0")}?redirectTo={HttpUtility.UrlEncode(redirectTo)}";
    }
}
