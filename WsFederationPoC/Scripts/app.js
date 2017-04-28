var WsFederationPoc;
(function (WsFederationPoc) {
    var Page = (function () {
        function Page() {

        }

        Page.init = function (siteInfo) {
            this._siteInfo = siteInfo;
            console.log(this._siteInfo);

            $(function () {
                var head = $("head");
                ["Content/fabric.min.css", "Content/fabric.components.min.css"].forEach(function (relUrl) {
                    head.append($("<link rel='stylesheet' type='text/css'>")
                        .attr("href", siteInfo.remoteAppUrl + "/" + relUrl));
                });

                var promises = [];
                ["Scripts/fabric.js"].forEach(function (scriptUrl) {
                    promises.push($.ajax({
                        cache: true,
                        url: siteInfo.remoteAppUrl + scriptUrl,
                        dataType: "script",
                        type: "GET"
                    }));
                });

                $.when.apply(null, promises).then(function () {
                    Page.remoteWebAjax("/api/demo/all", "GET", null, {})
                        .then(function (r) {
                            Page.renderResult(r, "#all");
                        })
                        .fail(function (e) {
                            console.error(JSON.parse(e.responseText).exceptionMessage);
                        });
                    Page.remoteWebAjax("/api/sharepoint/user/hostwebuser", "GET", null, {})
                        .then(function (r) {
                            Page.renderResult(r, "#hostwebuser");
                        })
                        .fail(function (e) {
                            console.error(JSON.parse(e.responseText).exceptionMessage);
                        });

                    Page.loadAppLists(null, "#applists");
                    Page.loadUserLists(null, "#userlists");
                    Page.createList("#createList");
                    Page.createItem("#createItem");
                    Page.createLoad("#createLoad");
                    Page.isInRole("#isinrole");
                    Page.roleMember("#rolemembers");
                });
            });
        };
        Page.remoteWebAjax = function (url, method, data, contentType) {
            return $.ajax({
                crossDomain: true,
                url: Page.removeTrailingSlash(this._siteInfo.remoteAppUrl) + url,
                dataType: "json",
                type: method,
                data: JSON.stringify(data),
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

        Page.renderResult = function (result, id) {
            $(function () {
                console.log(result);
                if ($.isArray(result)) {
                    result.forEach(
                        function (item) {
                            var div = $("<div>");
                            div.append($("<span>").text(item.id + ": "));
                            div.append($("<span>").text(item.name));
                            div.data("id", item.id);
                            $(id).append(div);
                            div.bind("click",
                                function (e) {
                                    var itemId = $(e.target).closest("div").data("id");
                                    Page.remoteWebAjax("/api/demo/" + itemId, "GET", null, {})
                                        .then(function (r) {
                                            Page.renderResult(r, "#item");
                                        });
                                });
                        });
                } else {
                    $(id).append($("<pre>").text(JSON.stringify(result)));
                }
            });
        };

        Page.isInRole = function (id) {
            Page.remoteWebAjax("/api/sharepoint/app/isinrole/Claims Members", "GET", null, {})
                .then(function (r) {
                    if (r) {
                        $(id).text("Current User is member of 'Claims Members'");
                    } else 
                    {
                        $(id).text("Current User is not a  member of 'Claims Members'");
                    }
                })
                .fail(function (e) {
                    console.error(JSON.parse(e.responseText).exceptionMessage);
                });
        }

        Page.roleMember = function (id) {
            Page.remoteWebAjax("/api/sharepoint/app/rolemembers/Claims Members", "GET", null, {})
                .then(function (r) {
                    if (r) {
                        $(id).append($("<pre>").text(JSON.stringify(r)));
                    } else {
                        $(id).append($("<pre>").text(JSON.stringify(r)));
                    }
                })
                .fail(function (e) {
                    console.error(JSON.parse(e.responseText).exceptionMessage);
                });
        }


        Page.loadAppLists = function (highlight, id) {
            Page.remoteWebAjax("/api/sharepoint/app/lists", "GET", null, {})
                .then(function (r) {
                    Page.renderAppLists(r, id, highlight);
                })
                .fail(function (e) {
                    console.error(JSON.parse(e.responseText).exceptionMessage);
                });

        };

        Page.loadUserLists = function (highlight, id, listId) {
            Page.remoteWebAjax("/api/sharepoint/user/lists", "GET", null, {})
                .then(function (r) {
                    Page.renderUserLists(r, id, highlight, listId);
                })
                .fail(function (e) {
                    console.error(JSON.parse(e.responseText).exceptionMessage);
                });

        };

        Page.renderAppLists = function (result, id, highlight) {
            var si = this._siteInfo;

            $(function () {
                $(id).empty();
                console.log(result);
                if ($.isArray(result)) {
                    var container;
                    $(id).append(container = $("<div class='ms-List ms-List--grid'>"));

                    result.forEach(
                        function (item) {
                            var div = $("<div class='ms-ListItem'>")
                                .addClass(item
                                    .title ===
                                    highlight
                                    ? "ms-bgColor-greenLight"
                                    : "ms-bgColor-themeLighter");
                            div.append(
                                $("<span class=\"ms-ListItem-primaryText\">").append(
                                    $("<a class='ms-Link'>").text(item.title).attr("href", si.SPHostUrl + item.url))
                            );
                            div.append($("<span class='ms-ListItem-secondaryText'>")
                                .text(item.itemcount + " Items"));
                            div.append($("<span class='ms-ListItem-tertiaryText'>")
                                .text(item.eventreceiverscount + " Event Receivers"));
                            div.append($("<span class='ms-ListItem-metaText'>")
                                .text(new Date(item.created).toLocaleString()));

                            div.append($("<div class='ms-ListItem-actions'>")
                                .append($("<div class='ms-ListItem-action'>").data("listId", item.id).bind("click",
                                        function (e) {
                                            var listId = $(e.target).closest("div").data("listId");
                                            console.log("Deleting: " + listId);
                                            Page.deleteList(listId, id);
                                        })
                                    .append($("<i class='ms-Icon ms-Icon--Cancel'>"))
                                )
                                .append($("<div class='ms-ListItem-action'>").data("listId", item.id).bind("click",
                                        function (e) {
                                            var listId = $(e.target).closest("div").data("listId");
                                            console.log("Attaching: " + listId);
                                            Page.attachListEventReceiver(listId, id);
                                        })
                                    .append($("<i class='ms-Icon ms-Icon--Attach'>")))
                            );
                            div.data("id", item.id);
                            container.append(div);
                        });
                }
            });
        };

        Page.renderUserLists = function (result, id, highlight, selected) {
            var si = this._siteInfo;

            $(function () {
                $(id).empty();
                console.log(result);
                if ($.isArray(result)) {
                    var container;
                    $(id).append(container = $("<ul class='ms-List'>"));

                    result.forEach(
                        function (item) {
                            var div = $("<li class='ms-ListItem is-read is-selectable'>").addClass(item
                                .title ===
                                highlight
                                ? "ms-bgColor-greenLight"
                                : "ms-bgColor-themeLighter").addClass(
                                item.id === selected ? "is-selected" : ""
                            );
                            div.append(
                                $("<span class=\"ms-ListItem-primaryText\">").append(
                                    $("<a class='ms-Link'>").text(item.title).attr("href", si.SPHostUrl + item.url))
                            );
                            div.append($("<span class='ms-ListItem-secondaryText'>")
                                .text(item.itemcount + " Items"));
                            div.append($("<span class='ms-ListItem-metaText'>")
                                .text(new Date(item.created).toLocaleString()));
                            div.append($("<div class='ms-ListItem-selectionTarget'>").bind("click",
                                    function (e) {
                                        var currentItem = $(e.target.closest("li"));
                                        if (currentItem.data("selected")) {
                                            currentItem.data("selected", false);
                                            $("#createItem").hide();
                                        } else {
                                            currentItem.data("selected", true);
                                            $("#createItem").show();
                                        }
                                        console.log(currentItem.data("selected"));
                                        currentItem.siblings().removeClass("is-selected");
                                    }))
                                .data("listid", item.id)
                                .data("selected", item.id === selected);
                            div.data("id", item.id);
                            container.append(div);
                        });
                    new fabric["List"](container.get(0));
                }
            });
        };

        Page.createList = function (id) {
            $(
                function () {
                    if (id) {
                        var textInput;
                        $(id).append(
                            $("<div class='ms-TextField'>")
                            .append($("<label class='ms-Label'>").text("List Title"))
                            .append(
                                textInput =
                                $("<input class='ms-TextField-field' type='text' placeholder='List Title ...'>")));

                        $(id).append($("<button>").addClass("ms-Button").append($("<span>").text("Create List"))
                            .bind("click",
                                function (e) {
                                    e.preventDefault();
                                    console.log("Creating: " + textInput.val());
                                    Page.remoteWebAjax("/api/sharepoint/app/create/list",
                                        "POST",
                                        {
                                            title: textInput.val()
                                        },
                                        {}).then(function (r) {
                                            Page.loadAppLists(textInput.val(), "#applists");
                                            Page.loadUserLists(textInput.val(), "#userlists");
                                        }).fail(function (e) {

                                            console.error(JSON.parse(e.responseText).exceptionMessage);
                                        });
                                }));
                    }
                }
            );
        };

        Page.createLoad = function (id) {
            $(
                function () {
                    var buttonTitle;
                    $(id).append($("<button>").addClass("ms-Button")
                        .append(buttonTitle = $("<span>").text("Create Load"))
                        .bind("click",
                            function (e) {
                                e.preventDefault();

                                buttonTitle.text("Running...").parent().prop("disabled", true);

                                Page.remoteWebAjax("/api/sharepoint/app/load",
                                    "GET",
                                    null,
                                    {}).then(function (r) {
                                        console.log(r);
                                        buttonTitle.text("Create Load").parent().prop("disabled", false);
                                    });
                            }));

                });
        };
        Page.createItem = function (id) {
            $(
                function () {
                    if (id) {
                        var textInput;
                        $(id).append(
                            $("<div class='ms-TextField'>")
                            .append($("<label class='ms-Label'>").text("Item Title"))
                            .append(
                                textInput =
                                $("<input class='ms-TextField-field' type='text' placeholder='Enter Title ...'>")));

                        $(id).append($("<button>").addClass("ms-Button")
                            .append($("<span>").text("Create List Item"))
                            .bind("click",
                                function (e) {
                                    e.preventDefault();
                                    console.log("Creating item: " + textInput.val());

                                    var listId = $("#userlists").find(".is-selected").data("listid");
                                    console.log("listid : " + listId);

                                    Page.remoteWebAjax("/api/sharepoint/user/create/listitem?listid=" + listId,
                                        "POST",
                                        {
                                            title: textInput.val()
                                        },
                                        {}).then(function (r) {
                                            Page.loadUserLists(textInput.val(), "#userlists", listId);
                                        });
                                }));
                    }
                }
            );
        };

        Page.deleteList = function (id, element) {
            Page.remoteWebAjax("/api/sharepoint/app/delete/list",
                    "POST",
                    {
                        id: id
                    },
                    {})
                .then(function (r) {
                    Page.loadAppLists(null, element);
                    Page.loadUserLists(null, "#userlists");
                });
        };

        Page.attachListEventReceiver = function (id, element) {
            Page.remoteWebAjax("/api/sharepoint/app/attach/listeventreceiver",
                "POST",
                {
                    id: id
                },
                {}).then(function (r) {
                    Page.loadAppLists(null, element);
                });
        };
        Page.removeTrailingSlash = function (url) {
            if (!url) return url;
            return (url[url.length - 1] === "/") ? url.substring(0, url.length - 1) : url;
        };

        Page.getQueryParameter = function (queryString, key) {
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
    }());;
    WsFederationPoc.Page = Page;
})(WsFederationPoc || (WsFederationPoc = {}));;


var subClass = new WsFederation.SubClass("p1");
console.log(subClass.p1);