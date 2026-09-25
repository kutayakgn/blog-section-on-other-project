namespace YkbYapikredi.Web.Services;

public sealed class AssetBundleService : IAssetBundleService
{

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

        return string.Join(Environment.NewLine, parts);
    }
}
