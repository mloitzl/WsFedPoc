var WsFederationPoc;
(function(WsFederationPoc) {
    var Page = (function() {
        function Page() {

        }

        Page.init = function(siteInfo) {
            this._siteInfo = siteInfo;
            console.log(this._siteInfo);

            Page.remoteWebAjax("/api/demo/all", "GET", null, {})
                .then(function(r) {
                    Page.renderResult(r, "#all");
                })
                .fail(function(e) {
                    console.error(e);
                });
            Page.remoteWebAjax("/api/sharepoint/hostwebuser", "GET", null, {})
                .then(function(r) {
                    Page.renderResult(r, "#hostwebuser");
                })
                .fail(function(e) {
                    console.error(e);
                });

        };

        Page.remoteWebAjax = function(url, method, data, contentType) {
            return $.ajax({
                crossDomain: true,
                url: Page.removeTrailingSlash(this._siteInfo.remoteAppUrl) + url,
                dataType: "json",
                type: method,
                data: data,
                headers: {
                    "Accept": "application/json;odata=verbose",
                    "Content-Type": "application/json;odata=verbose",
                    "SPHostUrl": this._siteInfo.SPHostUrl,
                    "SPLanguage": this._siteInfo.SPLanguage,
                    "SPClientTag": this._siteInfo.SPClientTag,
                    "SPProductNumber": this._siteInfo.SPProductNumber,
                },
                xhrFields: {
                    withCredentials: true
                }
            });
        };

        Page.renderResult = function(result, id) {
            $(function() {
                console.log(result);
                if ($.isArray(result)) {
                    result.forEach(
                        function(item) {
                            var div = $("<div>");
                            div.append($("<span>").text(item.id + ": "));
                            div.append($("<span>").text(item.name));
                            div.data("id", item.id);
                            $(id).append(div);
                            div.bind("click",
                                function(e) {
                                    var itemId = $(e.target).closest("div").data("id");
                                    Page.remoteWebAjax("/api/demo/" + itemId, "GET", null, {})
                                        .then(function(r) {
                                            Page.renderResult(r, "#item");
                                        });
                                });
                        });
                } else {
                    $(id).append($("<pre>").text(JSON.stringify(result)));
                }
            });
        };

        Page.removeTrailingSlash = function(url) {
            if (!url) return url;
            return (url[url.length - 1] === "/") ? url.substring(0, url.length - 1) : url;
        };

        Page.getQueryParameter = function(queryString, key) {
            if (queryString) {
                if (queryString[0] === "?") {
                    queryString = queryString.substring(1);
                }

                var keyValuePairArray = queryString.split("&");

                for (var i = 0; i < keyValuePairArray.length; i++) {
                    var currentKeyValuePair = keyValuePairArray[i].split("=");

                    if (currentKeyValuePair.length > 1 && currentKeyValuePair[0] === key) {
                        return currentKeyValuePair[1];
                    }
                }
            }

            return null;
        };
        return Page;
    }());
    WsFederationPoc.Page = Page;
})(WsFederationPoc || (WsFederationPoc = {}));