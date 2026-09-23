/*

    Blog Slider - Infinite Loop

    - Aktif kart büyük, diğerleri küçük.

    - 5 saniyede bir otomatik ilerler.

    - Gerçek sonsuz döngü:

        ... son -> ilk -> ikinci ...

    - Son karttan ilk karta GERİ ATLAMAZ;

      ilk kartın klonu son kartın hemen sağındadır.

    - Bullet'lar gerçek slide index'i ile senkron çalışır.

    - Bullet'a tıklanınca ilgili kart sola hizalanır.

    - Mouse drag desteklenir.

    - Touch cihazlarda native scroll kullanılabilir.

*/

(function () {

    "use strict";

    var sliders = document.querySelectorAll("[data-slider]");

    if (!sliders.length) {

        return;

    }

    sliders.forEach(function (slider) {

        initSlider(slider);

    });

    function initSlider(slider) {

        var track = slider.querySelector("[data-slider-track]");

        var prev = slider.querySelector("[data-slider-prev]");

        var next = slider.querySelector("[data-slider-next]");

        var dotList = slider.querySelector("[data-slider-dots]");

        if (!track) {

            return;

        }

        /*

         * ============================================================

         * ORIGINAL SLIDES

         * ============================================================

         */

        var originalSlides = Array.prototype.slice.call(

            track.querySelectorAll(".blog-slide")

        );

        var slideCount = originalSlides.length;

        if (!slideCount) {

            return;

        }

        var dots = dotList

            ? Array.prototype.slice.call(

                dotList.querySelectorAll("[data-slider-dot]")

            )

            : [];

        var AUTOPLAY_MS = parseInt(

            slider.getAttribute("data-slider-autoplay-ms") || "5000",

            10

        );

        var TRANSITION_MS = 450;

        var DRAG_THRESHOLD = 4;

        var autoplayTimer = 0;

        var scrollEndTimer = 0;

        var animationFrameId = 0;

        var animationToken = 0;

        var isAnimating = false;

        var pointerInteracting = false;

        var dragging = false;

        var captured = false;

        var moved = false;

        var startX = 0;

        var startScroll = 0;

        var suppressClickUntil = 0;

        var prefersReducedMotion =

            window.matchMedia &&

            window.matchMedia(

                "(prefers-reduced-motion: reduce)"

            ).matches;

        /*

         * Tek slide varsa loop oluşturmaya gerek yok.

         */

        if (slideCount === 1) {

            originalSlides[0].classList.add("is-active");

            if (prev) {

                prev.hidden = true;

            }

            if (next) {

                next.hidden = true;

            }

            if (dots[0]) {

                dots[0].classList.add("is-active");

                dots[0].setAttribute("aria-current", "true");

            }

            return;

        }

        /*

         * ============================================================

         * ORIGINAL INDEX'LER

         * ============================================================

         */

        originalSlides.forEach(function (slide, index) {

            slide.setAttribute(

                "data-slider-original-index",

                index

            );

        });

        /*

         * ============================================================

         * CLONE OLUŞTURMA

         * ============================================================

         *

         * DOM:

         *

         * [clone 1..N]

         * [original 1..N]

         * [clone 1..N]

         *

         * Böylece:

         *

         * ... 4 5 | 1 2 3 4 5 | 1 2 ...

         *

         * sağa doğru sonsuza kadar devam edebiliriz.

         */

        var prependFragment =

            document.createDocumentFragment();

        var appendFragment =

            document.createDocumentFragment();

        originalSlides.forEach(function (slide, index) {

            var beforeClone = slide.cloneNode(true);

            var afterClone = slide.cloneNode(true);

            prepareClone(beforeClone, index);

            prepareClone(afterClone, index);

            prependFragment.appendChild(beforeClone);

            appendFragment.appendChild(afterClone);

        });

        track.insertBefore(

            prependFragment,

            track.firstChild

        );

        track.appendChild(appendFragment);

        /*

         * Klonlar eklendikten sonraki bütün fiziksel slide'lar.

         *

         * 0 .. N-1       = baştaki clone seti

         * N .. 2N-1      = gerçek slide'lar

         * 2N .. 3N-1     = sondaki clone seti

         */

        var slides = Array.prototype.slice.call(

            track.querySelectorAll(".blog-slide")

        );

        /*

         * Başlangıç:

         *

         * İlk gerçek slide.

         */

        var physicalIndex = slideCount;

        var currentIndex = 0;

        /*

         * CSS'teki eski son boşluk sistemi infinite loop'ta

         * gerekli değil.

         */

        track.style.setProperty(

            "--slider-end-space",

            "0px"

        );

        /*

         * ============================================================

         * CLONE HAZIRLA

         * ============================================================

         */

        function prepareClone(clone, originalIndex) {

            clone.setAttribute(

                "data-slider-original-index",

                originalIndex

            );

            clone.setAttribute(

                "data-slider-clone",

                "true"

            );

            clone.setAttribute(

                "aria-hidden",

                "true"

            );

            clone.removeAttribute("aria-current");

            /*

             * Clone içindeki link/button gibi elemanlar

             * tab sırasına girmesin.

             */

            var focusableElements =

                clone.querySelectorAll(

                    "a, button, input, select, textarea, [tabindex]"

                );

            focusableElements.forEach(function (element) {

                element.setAttribute(

                    "tabindex",

                    "-1"

                );

            });

        }

        /*

         * ============================================================

         * HELPERS

         * ============================================================

         */

        function clamp(value, min, max) {

            return Math.max(

                min,

                Math.min(value, max)

            );

        }

        function easeInOutCubic(t) {

            return t < 0.5

                ? 4 * t * t * t

                : 1 -

                Math.pow(

                    -2 * t + 2,

                    3

                ) / 2;

        }

        function getLogicalIndex(slide) {

            if (!slide) {

                return 0;

            }

            var value = parseInt(

                slide.getAttribute(

                    "data-slider-original-index"

                ),

                10

            );

            return Number.isNaN(value)

                ? 0

                : value;

        }

        /*

         * Verilen fiziksel slide'ın sol başa gelmesi için

         * gerekli scrollLeft.

         */

        function getTargetScrollLeft(index) {

            var slide = slides[index];

            if (!slide) {

                return track.scrollLeft;

            }

            var trackRect =

                track.getBoundingClientRect();

            var slideRect =

                slide.getBoundingClientRect();

            return (

                track.scrollLeft +

                slideRect.left -

                trackRect.left

            );

        }

        /*

         * ============================================================

         * ACTIVE STATE

         * ============================================================

         *

         * Burada önemli nokta:

         *

         * Sadece görünen kopyayı değil, aynı logical index'e

         * sahip TÜM kopyaları active yapıyoruz.

         *

         * Örneğin index 0 aktifse:

         *

         * clone-0

         * original-0

         * clone-0

         *

         * üçünün de ölçüsü 514x617 olur.

         *

         * Böylece clone -> original teleport sırasında layout

         * değişmez ve kullanıcı hiçbir atlama görmez.

         */

        function setActive(logicalIndex) {

            currentIndex = logicalIndex;

            slides.forEach(function (slide) {

                var slideIndex =

                    getLogicalIndex(slide);

                slide.classList.toggle(

                    "is-active",

                    slideIndex === logicalIndex

                );

            });

            originalSlides.forEach(

                function (slide, index) {

                    if (index === logicalIndex) {

                        slide.setAttribute(

                            "aria-current",

                            "true"

                        );

                    } else {

                        slide.removeAttribute(

                            "aria-current"

                        );

                    }

                }

            );

            dots.forEach(function (dot, index) {

                var active =

                    index === logicalIndex;

                dot.classList.toggle(

                    "is-active",

                    active

                );

                if (active) {

                    dot.setAttribute(

                        "aria-current",

                        "true"

                    );

                } else {

                    dot.removeAttribute(

                        "aria-current"

                    );

                }

            });

            /*

             * Infinite loop'ta iki ok da sürekli kullanılabilir.

             */

            if (prev) {

                prev.hidden = false;

            }

            if (next) {

                next.hidden = false;

            }

        }

        /*

         * ============================================================

         * AUTOPLAY

         * ============================================================

         */

        function stopAutoplay() {

            if (!autoplayTimer) {

                return;

            }

            clearTimeout(autoplayTimer);

            autoplayTimer = 0;

        }

        function startAutoplay() {

            stopAutoplay();

            if (document.hidden) {

                return;

            }

            autoplayTimer =

                window.setTimeout(

                    function () {

                        moveBy(1);

                    },

                    AUTOPLAY_MS

                );

        }

        /*

         * ============================================================

         * ANIMATION CANCEL

         * ============================================================

         */

        function cancelAnimation() {

            animationToken++;

            if (animationFrameId) {

                cancelAnimationFrame(

                    animationFrameId

                );

                animationFrameId = 0;

            }

            isAnimating = false;

            track.classList.remove(

                "is-animating"

            );

        }

        /*

         * ============================================================

         * NORMALIZE / TELEPORT

         * ============================================================

         *

         * Clone bölgesine ulaştığımızda aynı kartın gerçek

         * karşılığına ANİMASYONSUZ taşınıyoruz.

         *

         * Kullanıcı bunu göremez çünkü:

         *

         * clone ve original aynı görüntüye,

         * aynı ölçüye ve aynı active state'e sahip.

         */

        function normalizePhysicalPosition() {

            var normalizedIndex =

                physicalIndex;

            /*

             * Baştaki clone setindeyiz.

             *

             * Örnek:

             *

             * physical N-1 clone son slide

             *

             * =>

             *

             * physical 2N-1 gerçek son slide

             */

            if (physicalIndex < slideCount) {

                normalizedIndex =

                    physicalIndex +

                    slideCount;

            }

            /*

             * Sondaki clone setindeyiz.

             *

             * physical 2N clone ilk slide

             *

             * =>

             *

             * physical N gerçek ilk slide

             */

            else if (

                physicalIndex >=

                slideCount * 2

            ) {

                normalizedIndex =

                    physicalIndex -

                    slideCount;

            }

            if (

                normalizedIndex ===

                physicalIndex

            ) {

                return;

            }

            physicalIndex =

                normalizedIndex;

            /*

             * Transition/smooth olmadan,

             * birebir aynı kopyaya geç.

             */

            track.classList.add(

                "is-jumping"

            );

            track.scrollLeft =

                getTargetScrollLeft(

                    physicalIndex

                );

            /*

             * Browser'a yeni pozisyonu çizdirdikten

             * sonra normal davranışı geri aç.

             */

            requestAnimationFrame(

                function () {

                    track.classList.remove(

                        "is-jumping"

                    );

                }

            );

        }

        /*

         * ============================================================

         * ANIMATE TO PHYSICAL SLIDE

         * ============================================================

         */

        function animateToPhysical(

            targetPhysicalIndex,

            restartAutoplay

        ) {

            targetPhysicalIndex = clamp(

                targetPhysicalIndex,

                0,

                slides.length - 1

            );

            cancelAnimation();

            physicalIndex =

                targetPhysicalIndex;

            var targetSlide =

                slides[physicalIndex];

            var logicalIndex =

                getLogicalIndex(

                    targetSlide

                );

            setActive(logicalIndex);

            /*

             * Reduced motion.

             */

            if (prefersReducedMotion) {

                track.scrollLeft =

                    getTargetScrollLeft(

                        physicalIndex

                    );

                normalizePhysicalPosition();

                if (restartAutoplay !== false) {

                    startAutoplay();

                }

                return;

            }

            isAnimating = true;

            track.classList.add(

                "is-animating"

            );

            var token =

                ++animationToken;

            var startedAt =

                performance.now();

            var startLeft =

                track.scrollLeft;

            function frame(now) {

                if (

                    token !==

                    animationToken

                ) {

                    return;

                }

                var elapsed =

                    now - startedAt;

                var progress =

                    clamp(

                        elapsed /

                        TRANSITION_MS,

                        0,

                        1

                    );

                var eased =

                    easeInOutCubic(

                        progress

                    );

                /*

                 * Aktif kart genişliği aynı anda

                 * 408 -> 514px oluyor.

                 *

                 * Bu yüzden hedef pozisyon transition

                 * boyunca değişir.

                 *

                 * Her frame hedefi yeniden hesaplıyoruz.

                 */

                var targetLeft =

                    getTargetScrollLeft(

                        physicalIndex

                    );

                track.scrollLeft =

                    startLeft +

                    (

                        targetLeft -

                        startLeft

                    ) *

                    eased;

                if (progress < 1) {

                    animationFrameId =

                        requestAnimationFrame(

                            frame

                        );

                    return;

                }

                /*

                 * Son pixel düzeltmesi.

                 */

                track.scrollLeft =

                    getTargetScrollLeft(

                        physicalIndex

                    );

                isAnimating = false;

                animationFrameId = 0;

                track.classList.remove(

                    "is-animating"

                );

                /*

                 * Eğer clone'a geldiysek aynı

                 * original slide'a görünmez şekilde geç.

                 */

                normalizePhysicalPosition();

                if (

                    restartAutoplay !== false

                ) {

                    startAutoplay();

                }

            }

            animationFrameId =

                requestAnimationFrame(

                    frame

                );

        }

        /*

         * ============================================================

         * NEXT / PREV

         * ============================================================

         */

        function moveBy(direction) {

            if (isAnimating) {

                return;

            }

            stopAutoplay();

            animateToPhysical(

                physicalIndex + direction,

                true

            );

        }

        /*

         * ============================================================

         * BULLET NAVIGATION

         * ============================================================

         */

        function goToLogical(

            logicalIndex

        ) {

            if (

                logicalIndex < 0 ||

                logicalIndex >= slideCount

            ) {

                return;

            }

            stopAutoplay();

            /*

             * Her normalize işleminden sonra fiziksel

             * pozisyon middle/original set içindedir.

             *

             * Bu yüzden target:

             *

             * N + logicalIndex

             */

            var targetPhysical =

                slideCount +

                logicalIndex;

            animateToPhysical(

                targetPhysical,

                true

            );

        }

        dots.forEach(

            function (dot, index) {

                dot.addEventListener(

                    "click",

                    function () {

                        goToLogical(

                            index

                        );

                    }

                );

            }

        );

        /*

         * ============================================================

         * ARROWS

         * ============================================================

         */

        if (prev) {

            prev.hidden = false;

            prev.addEventListener(

                "click",

                function () {

                    moveBy(-1);

                }

            );

        }

        if (next) {

            next.hidden = false;

            next.addEventListener(

                "click",

                function () {

                    moveBy(1);

                }

            );

        }
         /*         * ============================================================         * FIND NEAREST PHYSICAL SLIDE         * ============================================================         */function getNearestPhysicalIndex() {
            var trackRect =
                track.getBoundingClientRect();
            var bestIndex = 0;
            var bestDistance =
                Infinity;
            slides.forEach(
                function (slide, index) {
                    var slideRect =
                        slide.getBoundingClientRect();
                    var distance =
                        Math.abs(
                            slideRect.left -
                            trackRect.left                        );
                    if (
                        distance <
                        bestDistance                    ) {
                        bestDistance =
                            distance;
                        bestIndex =
                            index;
                    }
                }
            );
            return bestIndex;
        }
        /*         * ============================================================         * MOUSE DRAG         * ============================================================         */track.addEventListener(
            "pointerdown",
            function (event) {
                pointerInteracting =
                    true;
                stopAutoplay();
                if (isAnimating) {
                    cancelAnimation();
                }
                /*                 * Touch ve pen:                 * native browser scroll.                 */if (
                    event.pointerType !==
                    "mouse" ||
                    event.button !== 0
                ) {
                    return;
                }
                dragging = true;
                captured = false;
                moved = false;
                startX =
                    event.clientX;
                startScroll =
                    track.scrollLeft;
            }
        );
        track.addEventListener(
            "pointermove",
            function (event) {
                if (!dragging) {
                    return;
                }
                var delta =
                    event.clientX -
                    startX;
                if (
                    !moved &&
                    Math.abs(delta) >
                    DRAG_THRESHOLD                ) {
                    moved = true;
                    if (!captured) {
                        captured = true;
                        try {
                            track.setPointerCapture(
                                event.pointerId                            );
                        } catch (e) {
                            /*                             * Pointer capture desteklenmese                             * bile drag devam edebilir.                             */                        }
                        track.classList.add(
                            "is-dragging"                        );
                    }
                }
                if (!moved) {
                    return;
                }
                track.scrollLeft =
                    startScroll -
                    delta;
            }
        );
        function endPointerInteraction(
            event        ) {
            pointerInteracting =
                false;
            if (
                event.pointerType ===
                "mouse" &&
                dragging            ) {
                dragging = false;
                if (captured) {
                    captured = false;
                    track.classList.remove(
                        "is-dragging"                    );
                    try {
                        track.releasePointerCapture(
                            event.pointerId                        );
                    } catch (e) {
                        /*                         * Pointer zaten bırakılmış olabilir.                         */                    }
                }
                if (moved) {
                    suppressClickUntil =
                        Date.now() +
                        250;
                    var nearest =
                        getNearestPhysicalIndex();
                    animateToPhysical(
                        nearest,
                        true                    );
                    return;
                }
            }
            startAutoplay();
        }
        track.addEventListener(
            "pointerup",
            endPointerInteraction        );
        track.addEventListener(
            "pointercancel",
            endPointerInteraction        );
        /*         * Drag sonundaki click link açmasın.         */track.addEventListener(
            "click",
            function (event) {
                if (
                    Date.now() <
                    suppressClickUntil                ) {
                    event.preventDefault();
                    event.stopPropagation();
                }
            },
            true        );
        /*         * Görsel/link native drag kapalı.         */track.addEventListener(
            "dragstart",
            function (event) {
                event.preventDefault();
            }
        );
        /*         * ============================================================         * TOUCH / NATIVE SCROLL         * ============================================================         */track.addEventListener(
            "scroll",
            function () {
                if (
                    isAnimating ||
                    dragging ||
                    pointerInteracting                ) {
                    return;
                }
                clearTimeout(
                    scrollEndTimer                );
                scrollEndTimer =
                    window.setTimeout(
                        function () {
                            var nearest =
                                getNearestPhysicalIndex();
                            physicalIndex =
                                nearest;
                            var logical =
                                getLogicalIndex(
                                    slides[
                                    physicalIndex                                    ]
                                );
                            setActive(
                                logical                            );
                            normalizePhysicalPosition();
                            startAutoplay();
                        },
                        150
                    );
            },
            {
                passive: true            }
        );
        /*         * ============================================================         * RESIZE         * ============================================================         */window.addEventListener(
            "resize",
            function () {
                cancelAnimation();
                requestAnimationFrame(
                    function () {
                        track.scrollLeft =
                            getTargetScrollLeft(
                                physicalIndex                            );
                    }
                );
                startAutoplay();
            }
        );
        /*         * ============================================================         * VISIBILITY         * ============================================================         */document.addEventListener(
            "visibilitychange",
            function () {
                if (document.hidden) {
                    stopAutoplay();
                } else {
                    startAutoplay();
                }
            }
        );
        /*         * ============================================================         * INITIAL STATE         * ============================================================         */setActive(0);
        /*         * İlk render sırasında clone setinin ardından         * gerçek ilk slide'a animasyonsuz git.         */track.classList.add(
            "is-jumping"        );
        requestAnimationFrame(
            function () {
                track.scrollLeft =
                    getTargetScrollLeft(
                        physicalIndex                    );
                requestAnimationFrame(
                    function () {
                        track.classList.remove(
                            "is-jumping"                        );
                        startAutoplay();
                    }
                );
            }
        );
    }
})();
 