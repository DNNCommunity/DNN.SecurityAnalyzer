(function ($) {
    $.fn.dnnTabs = function (options) {
        var opts = $.extend({}, $.fn.dnnTabs.defaultOptions, options),
        $wrap = this;

        // patch for period in selector - http://jsfiddle.net/9Mst9/2/
        $.ui.tabs.prototype._sanitizeSelector = function (hash) {
            return hash.replace(/:/g, "\\:").replace(/\./g, "\\\.");
        };

        $wrap.each(function () {
            var showEvent, cookieId;
            if (this.id) {
                cookieId = 'dnnTabs-' + this.id;
                if (opts.selected === -1) {
                    var cookieValue = dnn.dom.getCookie(cookieId);
                    if (cookieValue) {
                        opts.selected = cookieValue;
                    }
                    if (opts.selected === -1) {
                        opts.selected = 0;
                    }
                }
                showEvent = (function (cid) {
                    return function (event, ui) {
                        dnn.dom.setCookie(cid, ui.newTab.index(), opts.cookieDays, '/', '', false, opts.cookieMilleseconds);
                    };
                })(cookieId);
            } else {
                showEvent = function () {
                };
            }

            $wrap.tabs({
                activate: showEvent,
                active: opts.selected,
                disabled: opts.disabled,
                fx: {
                    opacity: opts.opacity,
                    duration: opts.duration
                }
            });

            if (window.location.hash && window.location.hash != '#') {
                var substr = window.location.hash.substr(0, 50);
                $('a[href="' + encodeURI(substr) + '"]', $wrap).trigger('click');
            }

            // page validation integration - select tab that contain tripped validators
            if (typeof window.Page_ClientValidate != "undefined" && $.isFunction(window.Page_ClientValidate)) {
                $wrap.find(opts.validationTriggerSelector).click(function () {
                    if (!window.Page_ClientValidate(opts.validationGroup)) {
                        var invalidControl = $wrap.find(opts.invalidItemSelector).eq(0);
                        var $parent = invalidControl.closest(".ui-tabs-panel");
                        if ($parent.length > 0) {
                            var tabId = $parent.attr("id");
                            $parent.parent().find("a[href='#" + tabId + "']").click();
                        }
                    }
                });
            };
        });

        return $wrap;
    };

    $.fn.dnnTabs.defaultOptions = {
        opacity: 'toggle',
        duration: 'fast',
        validationTriggerSelector: '.dnnPrimaryAction',
        validationGroup: '',
        invalidItemSelector: '.dnnFormError[style*="inline"]',
        regionToToggleSelector: 'fieldset',
        selected: -1,
        cookieDays: 0,
        cookieMilleseconds: 1200000 // twenty minutes
    };

})(jQuery);