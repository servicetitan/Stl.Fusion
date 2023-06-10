(function() {

let openAuthWindow = function (action, flowName) {
    return FusionAuth.openAuthWindow(action, flowName);
}

window.FusionAuth = {
    schemas: "",
    windowTarget: "_blank",
    windowFeatures: "width=600,height=600",
    signInPath: "/signIn",
    signOutPath: "/signOut",
    closePath: "/fusion/close",
    mustRedirectOnPopupBlock: true,

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
        let popup = window.open(url, FusionAuth.windowTarget, FusionAuth.windowFeatures);
        // let popup = { closed: true };
        if (!popup || popup.closed || typeof popup.closed=='undefined') {
            if (this.mustRedirectOnPopupBlock) {
                let redirectUrl = window.location.href;
                let url = action + "?returnUrl=" + encode(redirectUrl);
                window.location.replace(url);
            }
            else {
                alert("Authentication popup is blocked by the browser. Please allow popups on this website and retry.")
            }
        }        
    }
};

})();
