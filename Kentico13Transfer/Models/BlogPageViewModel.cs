using YkbYapikredi.Application.Blog;

namespace YkbYapikredi.Web.Models;

/// <summary>
/// Blog kökü, üst kategori ve alt kategori listeleme sayfalarının modeli.
/// </summary>
public sealed class BlogPageViewModel : BlogLayoutViewModel
{
    public string Title { get; init; } = string.Empty;

    public IReadOnlyList<ArticleCardDto> Articles { get; init; } = [];

    /// <summary>
    /// Slider'daki son yazılar. Yalnızca blog kökünde doludur.
    /// </summary>
    public IReadOnlyList<ArticleCardDto> Latest { get; init; } = [];

    /// <summary>
    /// Üst kategori altındaki "Tümü" ve doğrudan alt kategori sekmeleri.
    /// Blog kökünde ve arama sonucunda boştur.
    /// </summary>
    public IReadOnlyList<BlogCategoryTabViewModel> CategoryTabs { get; init; } = [];

    public bool IsSearch => !string.IsNullOrWhiteSpace(Query);

    public bool HasCategoryTabs => !IsSearch && CategoryTabs.Count > 1;

    /// <summary>
    /// Arama sonucu eksik olabilir: dış servisin tarama penceresi dolmuştur.
    /// Yalnızca aramada anlamlıdır; listelemede her zaman false olur.
    /// </summary>
    public bool IsPartialCount { get; init; }

    public int PageNumber { get; init; } = 1;

    public int TotalPages { get; init; } = 1;

    public int TotalCount { get; init; }

    /// <summary>true ise en eski yazı baştadır.</summary>
    public bool OldestFirst { get; init; }

    /// <summary>
    /// Sıralamayı ters çeviren adres. Arama korunur, sayfa numarası sıfırlanır.
    /// </summary>
    public string SortToggleUrl => BuildUrl(pageNumber: 1, oldestFirst: !OldestFirst);

    /// <summary>Sayfalama bağlantısı. Arama ve sıralama korunur.</summary>
    public string PageUrl(int pageNumber) => BuildUrl(pageNumber, OldestFirst);

    /// <summary><see cref="PageNumbers"/> içinde "…" anlamına gelen değer.</summary>
    public const int PageGap = PagerNumbers.Gap;

    /// <summary>Sayfalamada görünecek numaralar (1 … 8 9 10 … 40).</summary>
    public IReadOnlyList<int> PageNumbers() => PagerNumbers.Build(PageNumber, TotalPages);

    /// <summary>
    /// Kartın üst kategorisini navigation verisinden bulur. Navigation zaten
    /// cache'te olduğu için burada kart başına yeni bir Kentico sorgusu oluşmaz.
    /// </summary>
    public BlogNavItem? GetCategory(ArticleCardDto article) =>
        Nav.FindCategory(article.AliasPath);

    private const string ListAnchor = "#yazilar";

    private string BuildUrl(int pageNumber, bool oldestFirst)
    {
        var parameters = new List<string>();

        if (!string.IsNullOrWhiteSpace(Query))
        {
            parameters.Add("q=" + Uri.EscapeDataString(Query));
        }

        // Aramada sıralama yoktur; dış servisin alaka sırasını koruruz.
        if (oldestFirst && !IsSearch)
        {
            parameters.Add("sort=asc");
        }

        if (pageNumber > 1)
        {
            parameters.Add("page=" + pageNumber);
        }

        var url = parameters.Count == 0
            ? BaseUrl
            : $"{BaseUrl}?{string.Join("&", parameters)}";

        return url + ListAnchor;
    }
}
