var WsFederationPoc;
(function (WsFederationPoc) {
    var Page = (function () {
        function Page() {

        }

        Page.init = function (siteInfo) {
            this._siteInfo = siteInfo;
            console.log(this._siteInfo);
            Page.remoteWebAjax("/api/demo/all", "GET", null, {})
                .then(function (r) {
                    Page.renderResult(r, "#all");
                })
                .fail(function (e) {
                    console.error(e);
                });
        };

        Page.remoteWebAjax = function (url, method, data, contentType) {
            return $.ajax({
                crossDomain: true,
                url: Page.removeTrailingSlash(this._siteInfo.remoteAppUrl) + url,
                dataType: "json",
                type: method,
                data: data,
                headers: {
                    "Accept": "application/json;odata=verbose",
                    "Content-Type": "application/json;odata=verbose"
                }
            });
        };

        Page.renderResult = function (result, id) {
            $(function () {
                console.log(result);
                $.isArray(result) &&
                    result.forEach(
                        function (item) {
                            $(id).append($("<div>")
                                .append("<span>").text(item.id).append("<span>").text(item.name));
                        });

            });
        };

        Page.removeTrailingSlash = function (url) {
            if (!url) return url;
            return (url[url.length - 1] === "/") ? url.substring(0, url.length - 1) : url;
        };

        return Page;
    }());
    WsFederationPoc.Page = Page;
})(WsFederationPoc || (WsFederationPoc = {}));