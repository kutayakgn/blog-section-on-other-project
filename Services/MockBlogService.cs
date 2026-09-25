using YkbYapikredi.Application.Blog;
using YkbYapikredi.Web.Models;

namespace YkbYapikredi.Web.Services;

public sealed class MockBlogService : IBlogService
{
    private const int PageSize = 6;

    private readonly IReadOnlyList<BlogArticle> _articles;
    private readonly IReadOnlyDictionary<string, BlogCategory> _categoriesBySlug;

    public MockBlogService()
    {
        Categories = CreateCategories();
        _categoriesBySlug = Categories
            .Where(category => !string.IsNullOrEmpty(category.Slug))
            .ToDictionary(category => category.Slug, StringComparer.OrdinalIgnoreCase);
        _articles = CreateArticles();
    }

    public IReadOnlyList<BlogCategory> Categories { get; }

    public BlogCategory? FindCategory(string slug) => _categoriesBySlug.GetValueOrDefault(slug);

    public BlogPageViewModel GetPage(BlogQuery query)
    {
        var normalizedQuery = query.Query?.Trim();
        var selectedCategory = string.IsNullOrWhiteSpace(query.CategorySlug)
            ? null
            : FindCategory(query.CategorySlug);

        IEnumerable<BlogArticle> filtered = _articles;

        if (selectedCategory is not null)
        {
            filtered = filtered.Where(article =>
                string.Equals(article.CategorySlug, selectedCategory.Slug, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(normalizedQuery))
        {
            filtered = filtered.Where(article =>
                article.Title.Contains(normalizedQuery, StringComparison.CurrentCultureIgnoreCase) ||
                article.Summary.Contains(normalizedQuery, StringComparison.CurrentCultureIgnoreCase) ||
                article.Content.Contains(normalizedQuery, StringComparison.CurrentCultureIgnoreCase));
        }

        filtered = query.OldestFirst
            ? filtered.OrderBy(article => article.PublishDate)
            : filtered.OrderByDescending(article => article.PublishDate);

        var filteredArticles = filtered.ToList();
        var totalPages = Math.Max(1, (int)Math.Ceiling(filteredArticles.Count / (double)PageSize));
        var pageNumber = Math.Clamp(query.Page, 1, totalPages);
        var pageBaseUrl = selectedCategory?.Url ?? "/blog";
        var isSearch = !string.IsNullOrWhiteSpace(normalizedQuery);
        var title = isSearch
            ? $"“{normalizedQuery}” için arama sonuçları"
            : selectedCategory?.Title ?? "Yapı Kredi Blog";

        return new BlogPageViewModel
        {
            Title = title,
            Articles = filteredArticles.Skip((pageNumber - 1) * PageSize).Take(PageSize).ToList(),
            Latest = _articles.OrderByDescending(article => article.PublishDate).Take(5).ToList(),
            Categories = _categoriesBySlug,
            PageBaseUrl = pageBaseUrl,
            SortToggleUrl = BuildSortUrl(pageBaseUrl, normalizedQuery, !query.OldestFirst),
            PageNumber = pageNumber,
            TotalPages = filteredArticles.Count == 0 ? 0 : totalPages,
            TotalCount = filteredArticles.Count,
            IsSearch = isSearch,
            IsPartialCount = false,
            OldestFirst = query.OldestFirst,
            BaseUrl = "/blog",
            Query = normalizedQuery,
            AccentColor = selectedCategory?.Color ?? "blue",
            Nav = CreateNav(selectedCategory?.Slug)
        };
    }

    public BlogArticleViewModel? GetArticle(string slug, string absoluteUrl)
    {
        var article = _articles.FirstOrDefault(item =>
            string.Equals(item.Slug, slug, StringComparison.OrdinalIgnoreCase));

        if (article is null)
        {
            return null;
        }

        var category = FindCategory(article.CategorySlug);
        var related = _articles
            .Where(item => item.Slug != article.Slug)
            .OrderByDescending(item => item.CategorySlug == article.CategorySlug)
            .ThenByDescending(item => item.PublishDate)
            .Take(3)
            .ToList();

        return new BlogArticleViewModel
        {
            Title = article.Title,
            Summary = article.Summary,
            Content = article.Content,
            CoverImageUrl = article.ImageUrl,
            AbsoluteUrl = absoluteUrl,
            PublishDate = article.PublishDate,
            ReadingMinutes = article.ReadingMinutes,
            Category = category,
            Embed = null,
            Breadcrumb =
            [
                new BlogBreadcrumbViewModel { Title = "Blog", Url = "/blog" },
                new BlogBreadcrumbViewModel { Title = category?.Title ?? "Yazılar", Url = category?.Url }
            ],
            Related = related,
            Categories = _categoriesBySlug,
            BaseUrl = "/blog",
            AccentColor = category?.Color ?? "blue",
            Nav = CreateNav(category?.Slug)
        };
    }

    private BlogNavViewModel CreateNav(string? activeSlug) => new()
    {
        Items = Categories,
        ActiveSlug = activeSlug
    };

    private static string BuildSortUrl(string baseUrl, string? query, bool oldestFirst)
    {
        var queryParts = new List<string>();

        if (!string.IsNullOrWhiteSpace(query))
        {
            queryParts.Add($"q={Uri.EscapeDataString(query)}");
        }

        if (oldestFirst)
        {
            queryParts.Add("sort=oldest");
        }

        return queryParts.Count == 0 ? baseUrl : $"{baseUrl}?{string.Join('&', queryParts)}";
    }

    private static IReadOnlyList<BlogCategory> CreateCategories() =>
    [
        new() { Slug = "", Title = "Tüm Yazılar", Color = "blue", IconCssClass = "icon-Book" },
        new() { Slug = "yasam", Title = "Yaşam", Color = "blue", IconCssClass = "icon-People" },
        new() { Slug = "teknoloji", Title = "Teknoloji", Color = "purple", IconCssClass = "icon-Laptop" },
        new() { Slug = "finans", Title = "Finans", Color = "red", IconCssClass = "icon-Chart" },
        new() { Slug = "surdurulebilirlik", Title = "Sürdürülebilirlik", Color = "green", IconCssClass = "icon-Tree" },
        new() { Slug = "kultur-sanat", Title = "Kültür & Sanat", Color = "lilac", IconCssClass = "icon-Book" },
        new() { Slug = "girisimcilik", Title = "Girişimcilik", Color = "orange", IconCssClass = "icon-Building" }
    ];

    private static IReadOnlyList<BlogArticle> CreateArticles() =>
    [
        Article("dijital-guvenlik-rehberi", "Dijital Dünyada Güvende Kalmanın 7 Yolu", "Günlük dijital alışkanlıklarınızı birkaç basit adımla daha güvenli hâle getirin.", "teknoloji", new DateTime(2026, 9, 18), 6),
        Article("butce-planlama", "Aylık Bütçe Planlamanın Pratik Yolları", "Gelir ve gider dengenizi korurken hedeflerinize alan açan yöntemler.", "finans", new DateTime(2026, 9, 11), 5),
        Article("sehirde-surdurulebilir-yasam", "Şehirde Sürdürülebilir Yaşam İçin Küçük Adımlar", "Evde, yolda ve alışverişte uygulanabilecek çevre dostu öneriler.", "surdurulebilirlik", new DateTime(2026, 9, 4), 7),
        Article("yapay-zeka-gunluk-hayat", "Yapay Zekâ Günlük Hayatımızı Nasıl Değiştiriyor?", "Akıllı asistanlardan kişiselleştirilmiş deneyimlere uzanan yeni gündem.", "teknoloji", new DateTime(2026, 8, 27), 8),
        Article("hafta-sonu-rotalari", "Şehre Yakın Beş Hafta Sonu Rotası", "Kısa bir molayla yenilenmek isteyenler için doğayla iç içe seçenekler.", "yasam", new DateTime(2026, 8, 19), 6),
        Article("girisim-fikrini-dogrulamak", "Girişim Fikrini Doğrulamanın İlk Adımları", "Fikrinizi yatırıma dönüştürmeden önce doğru soruları nasıl sorarsınız?", "girisimcilik", new DateTime(2026, 8, 8), 9),
        Article("evde-sanat-kosesi", "Evinizde İlham Veren Bir Sanat Köşesi Kurun", "Küçük alanlarda kişisel ve yaratıcı bir atmosfer oluşturma rehberi.", "kultur-sanat", new DateTime(2026, 7, 29), 4),
        Article("birikim-aliskanligi", "Birikim Alışkanlığı Kazanmak İçin 30 Gün", "Küçük ama düzenli adımlarla sürdürülebilir birikim rutini oluşturun.", "finans", new DateTime(2026, 7, 17), 5),
        Article("uzaktan-calisma", "Verimli Uzaktan Çalışma İçin Dijital Araçlar", "Ekip iletişimini ve kişisel odağı destekleyen araçları birlikte inceleyelim.", "teknoloji", new DateTime(2026, 7, 3), 7),
        Article("atiklari-azaltmak", "Evsel Atıkları Azaltmanın Yaratıcı Yolları", "Yeniden kullanım ve doğru planlamayla daha az atık üretmek mümkün.", "surdurulebilirlik", new DateTime(2026, 6, 22), 6),
        Article("okuma-listesi", "Yaz Akşamları İçin Beş Kitap", "Farklı türlerden, yeni düşüncelere kapı açan kısa bir okuma listesi.", "kultur-sanat", new DateTime(2026, 6, 9), 4),
        Article("saglikli-sabah-rutini", "Güne İyi Başlatan Sade Bir Sabah Rutini", "Enerjinizi koruyan ve günün temposuna hazırlayan uygulanabilir öneriler.", "yasam", new DateTime(2026, 5, 28), 5),
        Article("sosyal-girisimcilik", "Sosyal Girişimcilik Neden Yükseliyor?", "Finansal sürdürülebilirlikle toplumsal etkiyi buluşturan yeni yaklaşım.", "girisimcilik", new DateTime(2026, 5, 16), 8),
        Article("finansal-hedefler", "Finansal Hedefleri Gerçeğe Dönüştürme Rehberi", "Ölçülebilir hedefler belirleyip ilerlemenizi görünür kılmanın yolları.", "finans", new DateTime(2026, 5, 2), 7)
    ];

    private static BlogArticle Article(
        string slug,
        string title,
        string summary,
        string categorySlug,
        DateTime publishDate,
        int readingMinutes) => new()
        {
            Slug = slug,
            Title = title,
            Summary = summary,
            CategorySlug = categorySlug,
            PublishDate = publishDate,
            ReadingMinutes = readingMinutes,
            ImageUrl = $"/mock/blog/{slug}.svg",
            Content = $$"""
                <p>{{summary}}</p>
                <p>Değişen ihtiyaçlara uyum sağlamak, çoğu zaman büyük kararlar yerine doğru zamanda atılan küçük adımlarla başlar. Bu rehberde gündelik hayata kolayca eklenebilecek önerileri bir araya getirdik.</p>
                <h2>Nereden başlamalı?</h2>
                <p>Önce mevcut alışkanlıklarınızı gözlemleyin ve geliştirmek istediğiniz tek bir alan seçin. Açık, ölçülebilir ve gerçekçi bir hedef belirlemek ilerlemeyi kolaylaştırır.</p>
                <ul>
                    <li>İhtiyacınızı ve önceliğinizi netleştirin.</li>
                    <li>Küçük bir deneme süresi belirleyin.</li>
                    <li>Sonuçları düzenli aralıklarla değerlendirin.</li>
                </ul>
                <blockquote><p>Sürdürülebilir değişim, tekrar edilebilen küçük adımların toplamıdır.</p></blockquote>
                <h2>İlerlemenizi görünür kılın</h2>
                <p>Haftalık kısa notlar tutmak, hangi yaklaşımın işe yaradığını fark etmenizi sağlar. İhtiyaçlarınız değiştikçe planınızı da güncelleyebilirsiniz.</p>
                """
        };
}
