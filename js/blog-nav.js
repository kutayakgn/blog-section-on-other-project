/* Blog sidebar ve mobil header etkileşimleri. */
(function () {
    "use strict";
    var STORAGE_KEY = "ykb-blog-nav-closed";
    var DESKTOP_CLOSED_CLASS = "blog--nav-closed";
    var MOBILE_OPEN_CLASS = "blog--mobile-nav-open";
    var SEARCH_OPEN_CLASS = "blog--search-open";
    var MOBILE_QUERY = "(max-width: 991px)";
    var body = document.body;
    var desktopToggle = document.querySelector("[data-desktop-nav-toggle]");
    var mobileToggle = document.querySelector("[data-mobile-nav-toggle]");
    var mobileDismiss = document.querySelector("[data-mobile-nav-dismiss]");
    var searchToggle = document.querySelector("[data-search-toggle]");
    var searchCloseButtons = document.querySelectorAll("[data-search-close]");
    var searchInput = document.querySelector(".blog-search__input");
    var scrollTopButton = document.querySelector("[data-scroll-top]");
    var mobileMedia = window.matchMedia ? window.matchMedia(MOBILE_QUERY) : null;
    if (!body) { return; }
    try {
        if (localStorage.getItem(STORAGE_KEY) === "1") {
            body.classList.add(DESKTOP_CLOSED_CLASS);
        }
    } catch (e) {
        /* localStorage kullanılamıyorsa menü varsayılan açık kalır. */
    }
    function isMobile() {
        return mobileMedia ? mobileMedia.matches : window.innerWidth < 992;
    }
    function syncDesktopToggle() {
        if (!desktopToggle) { return; }
        var closed = body.classList.contains(DESKTOP_CLOSED_CLASS);
        desktopToggle.setAttribute("aria-expanded", closed ? "false" : "true");
        desktopToggle.setAttribute("aria-label", closed ? "Menüyü genişlet" : "Menüyü daralt");
    }
    function setMobileNav(open) {
        body.classList.toggle(MOBILE_OPEN_CLASS, open);
        if (mobileToggle) {
            mobileToggle.setAttribute("aria-expanded", open ? "true" : "false");
            mobileToggle.setAttribute("aria-label", open ? "Blog menüsünü kapat" : "Blog menüsünü aç");
        }
    }
    function setSearch(open) {
        body.classList.toggle(SEARCH_OPEN_CLASS, open);
        if (searchToggle) {
            searchToggle.setAttribute("aria-expanded", open ? "true" : "false");
            searchToggle.setAttribute("aria-label", open ? "Aramayı kapat" : "Aramayı aç");
        }
        if (open && searchInput) {
            window.setTimeout(function () { searchInput.focus(); }, 310);
        }
    }
    syncDesktopToggle();
    if (desktopToggle) {
        desktopToggle.addEventListener("click", function () {
            var closed = body.classList.toggle(DESKTOP_CLOSED_CLASS);
            syncDesktopToggle();
            try { localStorage.setItem(STORAGE_KEY, closed ? "1" : "0"); } catch (e) {}
        });
    }
    if (mobileToggle) {
        mobileToggle.addEventListener("click", function () {
            setSearch(false);
            setMobileNav(!body.classList.contains(MOBILE_OPEN_CLASS));
        });
    }
    if (mobileDismiss) {
        mobileDismiss.addEventListener("click", function () { setMobileNav(false); });
    }
    if (searchToggle) {
        searchToggle.addEventListener("click", function () {
            setMobileNav(false);
            setSearch(!body.classList.contains(SEARCH_OPEN_CLASS));
        });
    }
    Array.prototype.forEach.call(searchCloseButtons, function (searchClose) {
        searchClose.addEventListener("click", function () {
            setSearch(false);
            searchToggle && searchToggle.focus();
        });
    });
    if (scrollTopButton) {
        var scrollTicking = false;
        var syncScrollTopButton = function () {
            scrollTopButton.hidden = window.scrollY <= 80;
            scrollTicking = false;
        };

        window.addEventListener("scroll", function () {
            if (scrollTicking) { return; }
            scrollTicking = true;
            window.requestAnimationFrame(syncScrollTopButton);
        }, { passive: true });

        scrollTopButton.addEventListener("click", function (event) {
            event.preventDefault();
            var reduceMotion = window.matchMedia
                && window.matchMedia("(prefers-reduced-motion: reduce)").matches;
            window.scrollTo({ top: 0, behavior: reduceMotion ? "auto" : "smooth" });
        });

        syncScrollTopButton();
    }
    document.addEventListener("keydown", function (event) {
        if (event.key !== "Escape") { return; }
        if (body.classList.contains(SEARCH_OPEN_CLASS)) {
            setSearch(false);
            searchToggle && searchToggle.focus();
        } else if (body.classList.contains(MOBILE_OPEN_CLASS)) {
            setMobileNav(false);
            mobileToggle && mobileToggle.focus();
        }
    });
    function handleBreakpointChange() {
        if (!isMobile()) {
            setMobileNav(false);
            setSearch(false);
        }
    }
    if (mobileMedia && mobileMedia.addEventListener) {
        mobileMedia.addEventListener("change", handleBreakpointChange);
    } else {
        window.addEventListener("resize", handleBreakpointChange);
    }
})();
