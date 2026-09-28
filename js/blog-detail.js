/*
    Yazı sayfası: paylaşım menüsü ve yazı boyutu.
    Vanilla — jQuery ya da başka bağımlılık yok.
*/
(function () {
    "use strict";
    /* ---------- Paylaşım ---------- */
    var share = document.querySelector("[data-share]");
    if (share) {
        var popupLinks = share.querySelectorAll("[data-share-popup]");
        var copy = share.querySelector("[data-share-copy]");

        Array.prototype.forEach.call(popupLinks, function (link) {
            link.addEventListener("click", function (event) {
                event.preventDefault();

                var width = 640;
                var height = 520;
                var screenLeft = window.screenX || window.screenLeft || 0;
                var screenTop = window.screenY || window.screenTop || 0;
                var left = Math.max(0, screenLeft + (window.outerWidth - width) / 2);
                var top = Math.max(0, screenTop + (window.outerHeight - height) / 2);
                var platform = link.getAttribute("data-share-popup") || "platform";
                var features = [
                    "popup=yes",
                    "width=" + width,
                    "height=" + height,
                    "left=" + Math.round(left),
                    "top=" + Math.round(top),
                    "resizable=yes",
                    "scrollbars=yes",
                    "toolbar=no",
                    "menubar=no",
                    "status=no"
                ].join(",");

                var popup = window.open(
                    link.href,
                    "ykbBlogShare_" + platform,
                    features);

                if (popup) {
                    popup.focus();
                }
            });
        });

        if (copy) {
            copy.addEventListener("click", function () {
                var url = share.getAttribute("data-share-url");
                var original = copy.textContent;
                function done() {
                    copy.textContent = "Kopyalandı";
                    setTimeout(function () { copy.textContent = original; }, 1600);
                }
                if (navigator.clipboard) {
                    navigator.clipboard.writeText(url).then(done).catch(function () {});
                    return;
                }
                // clipboard API yoksa (eski tarayıcı / güvensiz origin) geçici alan.
                var field = document.createElement("input");
                field.value = url;
                document.body.appendChild(field);
                field.select();
                try { document.execCommand("copy"); done(); } catch (e) {}
                document.body.removeChild(field);
            });
        }
    }
    /* ---------- Yazı boyutu ---------- */
    var text = document.querySelector("[data-article-text]");
    if (!text) {
        return;
    }
    var STORAGE_KEY = "ykb-blog-font-size";
    var SIZES = ["is-size-sm", "", "is-size-lg"];
    var index = 1;
    function apply() {
        SIZES.forEach(function (cls) {
            if (cls) {
                text.classList.remove(cls);
            }
        });
        if (SIZES[index]) {
            text.classList.add(SIZES[index]);
        }
        try {
            localStorage.setItem(STORAGE_KEY, String(index));
        } catch (e) {
            // localStorage kapalıysa tercih kalıcı olmaz; boyut yine değişir.
        }
    }
    try {
        var saved = parseInt(localStorage.getItem(STORAGE_KEY), 10);
        if (saved >= 0 && saved < SIZES.length) {
            index = saved;
            apply();
        }
    } catch (e) {}
    var smaller = document.querySelector("[data-font-smaller]");
    var larger = document.querySelector("[data-font-larger]");
    if (smaller) {
        smaller.addEventListener("click", function () {
            index = Math.max(0, index - 1);
            apply();
        });
    }
    if (larger) {
        larger.addEventListener("click", function () {
            index = Math.min(SIZES.length - 1, index + 1);
            apply();
        });
    }
})();
