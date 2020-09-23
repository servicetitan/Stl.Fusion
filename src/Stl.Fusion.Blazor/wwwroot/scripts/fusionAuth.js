(function() {

let openAuthWindow = function (action, flowName) {
    return FusionAuth.openAuthWindow(action, flowName);
}

window.FusionAuth = {
    sessionId: "",
    windowTarget: "_blank",
    windowFeatures: "width=600,height=600",
    login: function () {
        openAuthWindow("login", "Sign-in");
    },
    logout: function () {
        openAuthWindow("logout", "Sign-out");
    },
    openAuthWindow: function (action, flowName) {
        let encode = encodeURIComponent;
        let redirectUrl = new URL("/close?flow=" + encode(flowName), document.baseURI).href;
        let url = "/" + action + "?returnUrl=" + encode(redirectUrl);
        return window.open(url, FusionAuth.windowTarget, FusionAuth.windowFeatures);
    }
};

})();
