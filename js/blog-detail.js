/*
    Yazı sayfası: paylaşım menüsü ve yazı boyutu.
    Vanilla — jQuery ya da başka bağımlılık yok.
*/
(function () {
    "use strict";
    /* ---------- Paylaşım ---------- */
    var share = document.querySelector("[data-share]");
    if (share) {
        var toggle = share.querySelector(".blog-share__toggle");
        var copy = share.querySelector("[data-share-copy]");
        if (toggle) {
            toggle.addEventListener("click", function () {
                var url = share.getAttribute("data-share-url");
                var title = share.getAttribute("data-share-title");
                // Destekleyen cihazda işletim sisteminin paylaşım ekranı açılır.
                if (navigator.share) {
                    navigator.share({ title: title, url: url }).catch(function () {
                        // Kullanıcı vazgeçti; yapılacak bir şey yok.
                    });
                    return;
                }
                var open = share.classList.toggle("is-open");
                toggle.setAttribute("aria-expanded", open ? "true" : "false");
            });
        }
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
        // Dışarı tıklayınca liste kapansın.
        document.addEventListener("click", function (event) {
            if (!share.contains(event.target)) {
                share.classList.remove("is-open");
                if (toggle) {
                    toggle.setAttribute("aria-expanded", "false");
                }
            }
        });
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
