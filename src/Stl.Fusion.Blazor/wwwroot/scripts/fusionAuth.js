(function() {

let openAuthWindow = function (action, flowName) {
    return FusionAuth.openAuthWindow(action, flowName);
}

window.FusionAuth = {
    authContextId: "",
    windowTarget: "_blank",
    windowFeatures: "width=600,height=600",
    signIn: function () {
        openAuthWindow("signin", "Sign-in");
    },
    signOut: function () {
        openAuthWindow("signout", "Sign-out");
    },
    openAuthWindow: function (action, flowName) {
        let encode = encodeURIComponent;
        let redirectUrl = new URL("/close?flow=" + encode(flowName), document.baseURI).href;
        let url = "/" + action + "?returnUrl=" + encode(redirectUrl);
        return window.open(url, FusionAuth.windowTarget, FusionAuth.windowFeatures);
    }
};

})();
