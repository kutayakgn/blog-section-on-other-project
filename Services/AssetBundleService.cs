namespace YkbYapikredi.Web.Services;

public sealed class AssetBundleService : IAssetBundleService
{
    private const string CompatibilityCss = """
        .blog-pager__page {
            box-sizing: border-box;
            width: 32px;
            min-width: 32px;
            height: 32px;
            padding: 0;
        }

        @media (min-width: 992px) {
            .blog {
                --blog-desktop-content-right: 238px;
            }

            .blog-layout {
                grid-template-columns: var(--blog-nav-width) minmax(0, 1fr);
                grid-template-areas:
                    "nav main"
                    "footer footer";
                column-gap: 56px;
            }

            .blog--nav-closed .blog-layout {
                grid-template-columns: var(--blog-nav-width-closed) minmax(0, 1fr);
            }

            .blog-search {
                max-width: none;
                margin-right: calc(var(--blog-desktop-content-right) - 35px);
            }

            .blog-nav__link {
                justify-content: flex-start;
                gap: 24px;
                padding-right: 24px;
                padding-left: 24px;
            }

            .blog--nav-closed .blog-nav__link {
                justify-content: center;
                gap: 0;
                padding-right: 24px;
                padding-left: 24px;
            }

            .blog-nav {
                width: var(--blog-nav-width);
            }

            .blog--nav-closed .blog-nav {
                width: var(--blog-nav-width-closed);
            }

            .blog-nav__inner {
                position: fixed;
                top: 151px;
                left: 24px;
                z-index: 70;
                box-sizing: border-box;
                width: var(--blog-nav-width);
                max-height: calc(100vh - 151px);
                overflow-x: hidden;
                overflow-y: auto;
                overscroll-behavior: contain;
                scrollbar-width: none;
                transition: width .35s cubic-bezier(.22, .61, .36, 1);
            }

            .blog--nav-closed .blog-nav__inner {
                width: var(--blog-nav-width-closed);
            }

            .blog-nav__inner::-webkit-scrollbar {
                display: none;
            }

            .blog-nav__list {
                width: var(--blog-nav-width);
                transition: width .35s cubic-bezier(.22, .61, .36, 1);
            }

            .blog--nav-closed .blog-nav__list {
                width: var(--blog-nav-width-closed);
            }

            .blog-cards,
            .blog--nav-closed .blog-cards {
                grid-template-columns: repeat(auto-fit, minmax(min(390px, 100%), 1fr));
                width: auto;
                max-width: none;
                margin-right: var(--blog-desktop-content-right);
                transition: max-width .35s cubic-bezier(.22, .61, .36, 1);
            }

            .blog-section-head,
            .blog--nav-closed .blog-section-head {
                width: auto;
                max-width: none;
                margin-right: var(--blog-desktop-content-right);
            }

            .blog-footer,
            .blog--nav-closed .blog-footer {
                grid-column: 1 / -1;
                box-sizing: border-box;
                width: auto;
                max-width: none;
                margin-left: -24px;
                padding-right: 35px;
                padding-left: 36px;
                transition: none;
            }

            .blog--nav-closed .blog-cards,
            .blog--nav-closed .blog-section-head {
                max-width: 100%;
            }
        }

        @media (min-width: 769px) and (max-width: 1024px) {
            .blog-slide,
            .blog-slide.is-active {
                flex-basis: min(408px, calc(100vw - 64px));
                width: min(408px, calc(100vw - 64px));
            }

            .blog-slide__link,
            .blog-slide.is-active .blog-slide__link {
                height: 490px;
            }

            .blog-slider__track {
                min-height: 490px;
                max-height: 490px;
            }
        }

        @media (max-width: 991px) {
            html,
            .blog-layout {
                overflow-x: clip;
            }

            .blog-section-head,
            .blog-cards,
            .blog-footer {
                box-sizing: border-box;
                width: 100%;
                max-width: 100%;
            }

            .blog-header {
                overflow-x: clip;
            }
        }

        @media (min-width: 425px) and (max-width: 768px) {
            .blog-slide,
            .blog-slide.is-active {
                flex: 0 0 min(394px, 100%);
                width: min(394px, 100%);
            }

            .blog-slide__link,
            .blog-slide.is-active .blog-slide__link {
                height: auto;
                aspect-ratio: 394 / 474;
            }

            .blog-slider__track {
                min-height: 0;
                max-height: none;
            }
        }

        @media (max-width: 424px) {
            .blog-slide,
            .blog-slide.is-active {
                flex: 0 0 100%;
                width: 100%;
            }

            .blog-slide__link,
            .blog-slide.is-active .blog-slide__link {
                height: auto;
                aspect-ratio: 394 / 474;
            }

            .blog-slider__track {
                min-height: 0;
                max-height: none;
            }

            .blog-slider__arrow {
                display: none;
            }
        }
        """;

    private static readonly string[] CssFiles =
    [
        "blog-base.css",
        "blog-icomoon.css",
        "blog-header.css",
        "blog-nav.css",
        "blog-slider.css",
        "blog-card.css",
        "blog-pager.css",
        "blog-detail.css",
        "blog-footer.css"
    ];

    private static readonly string[] JavaScriptFiles =
    [
        "blog-nav.js",
        "blog-slider.js",
        "blog-detail.js"
    ];

    private readonly string _contentRootPath;
    private readonly Lazy<Task<string>> _css;
    private readonly Lazy<Task<string>> _javaScript;

    public AssetBundleService(IHostEnvironment environment)
    {
        _contentRootPath = environment.ContentRootPath;
        _css = new Lazy<Task<string>>(() => ReadBundleAsync("css", CssFiles));
        _javaScript = new Lazy<Task<string>>(() => ReadBundleAsync("js", JavaScriptFiles));
    }

    public Task<string> GetCssAsync() => _css.Value;

    public Task<string> GetJavaScriptAsync() => _javaScript.Value;

    private async Task<string> ReadBundleAsync(string directory, IEnumerable<string> fileNames)
    {
        var parts = new List<string>();

        foreach (var fileName in fileNames)
        {
            var path = Path.Combine(_contentRootPath, directory, fileName);
            parts.Add(await File.ReadAllTextAsync(path));
        }

        if (directory == "css")
        {
            parts.Add(CompatibilityCss);
        }

        return string.Join(Environment.NewLine, parts);
    }
}
