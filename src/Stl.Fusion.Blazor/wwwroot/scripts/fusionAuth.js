(function() {

let openAuthWindow = function (action, flowName) {
    return FusionAuth.openAuthWindow(action, flowName);
}

window.FusionAuth = {
    schemas: "",
    sessionId: "",
    windowTarget: "_blank",
    windowFeatures: "width=600,height=600",
    signInPath: "/signIn",
    signOutPath: "/signOut",
    closePath: "/fusion/close",

    signIn: function (schema) {
        if (schema === undefined || schema === null || schema === "") {
            openAuthWindow(this.signInPath, "Sign-in");
        } else {
            openAuthWindow(this.signInPath + "/" + schema, "Sign-in");
        }
    },

    signOut: function () {
        openAuthWindow(this.signOutPath, "Sign-out");
    },

    openAuthWindow: function (action, flowName) {
        let encode = encodeURIComponent;
        let redirectUrl = new URL(this.closePath +"?flow=" + encode(flowName), document.baseURI).href;
        let url = action + "?returnUrl=" + encode(redirectUrl);
        return window.open(url, FusionAuth.windowTarget, FusionAuth.windowFeatures);
    }
};

})();
