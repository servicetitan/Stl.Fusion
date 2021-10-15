using System.Web;
using Microsoft.AspNetCore.Components;
using Stl.OS;

namespace Stl.Fusion.Blazor;

public class BlazorModeHelper
{
    public static bool IsServerSideBlazor { get; } = OSInfo.Kind != OSKind.WebAssembly;

    protected NavigationManager Navigator { get; }

    public BlazorModeHelper(NavigationManager navigator)
        => Navigator = navigator;

    public virtual bool ChangeMode(bool isServerSideBlazor)
    {
        if (IsServerSideBlazor == isServerSideBlazor)
            return false;
        var switchUrl = GetModeChangeUrl(isServerSideBlazor, Navigator.Uri);
        Navigator.NavigateTo(switchUrl, true);
        return true;
    }

    public virtual string GetModeChangeUrl(bool isServerSideBlazor, string? redirectTo = null)
    {
        redirectTo ??= "/";
        var isSsbString = isServerSideBlazor.ToString().ToLowerInvariant();
        return $"/fusion/blazorMode/{isSsbString}?redirectTo={HttpUtility.UrlEncode(redirectTo)}";
    }
}
