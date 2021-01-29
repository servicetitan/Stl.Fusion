using System;
using System.Web;
using Microsoft.AspNetCore.Components;
using Stl.OS;

namespace Stl.Fusion.Blazor
{
    public class FusionBlazorModeHelper
    {
        public static bool IsServerSideBlazor { get; } = OSInfo.Kind != OSKind.WebAssembly;

        public static bool ChangeMode(NavigationManager navigator, bool isServerSideBlazor)
        {
            if (IsServerSideBlazor == isServerSideBlazor)
                return false;
            var switchUrl = GetModeChangeUrl(isServerSideBlazor, navigator.Uri);
            navigator.NavigateTo(switchUrl, true);
            return true;
        }

        public static string GetModeChangeUrl(bool isServerSideBlazor, string? redirectTo = null)
        {
            redirectTo ??= "/";
            var isSsbString = isServerSideBlazor.ToString().ToLowerInvariant();
            return $"/fusion/blazorMode/{isSsbString}?redirectTo={HttpUtility.UrlEncode(redirectTo)}";
        }
    }
}
