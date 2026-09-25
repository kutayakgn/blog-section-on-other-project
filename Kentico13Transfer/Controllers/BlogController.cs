using CMS.DocumentEngine;
using CMS.Helpers;
using CMS.SiteProvider;
using Kentico.Content.Web.Mvc;
using Kentico.Content.Web.Mvc.Routing;
using Microsoft.AspNetCore.Mvc;
using YkbYapikredi.Application.Blog;
using YkbYapikredi.Application.Search;
using YkbYapikredi.Web.Controllers;
using YkbYapikredi.Web.Filters;
using YkbYapikredi.Web.Models;
using CmsBlogCategory = CMS.DocumentEngine.Types.YkbYapikredi.YkbBlogCategory;

[assembly: RegisterPageRoute("YkbYapikredi.Blog", typeof(BlogController))]
[assembly: RegisterPageRoute(CmsBlogCategory.CLASS_NAME, typeof(BlogController))]

namespace YkbYapikredi.Web.Controllers;

[SkipMainLayoutData]
public sealed class BlogController : BaseController
{
    private const int PageSize = 6;
    private const int LatestCount = 10;
    private const int MaxQueryLength = 50;
    private const string BlogRootPath = "/blog";
    private const string PageTitleField = "PageTitle";

    private readonly IBlogNavigationService navigationService;
    private readonly IBlogArticleService articleService;
    private readonly ISiteSearchService siteSearch;
    private readonly IPageRetriever pageRetriever;
    private readonly IPageUrlRetriever pageUrlRetriever;

    public BlogController(
        IPageDataContextRetriever pageDataContextRetriever,
        IBlogNavigationService navigationService,
        IBlogArticleService articleService,
        ISiteSearchService siteSearch,
        IPageRetriever pageRetriever,
        IPageUrlRetriever pageUrlRetriever)
        : base(pageDataContextRetriever)
    {
        this.navigationService = navigationService;
        this.articleService = articleService;
        this.siteSearch = siteSearch;
        this.pageRetriever = pageRetriever;
        this.pageUrlRetriever = pageUrlRetriever;
    }

    public async Task<IActionResult> Index(
        [FromQuery] string? q = null,
        [FromQuery] string? sort = null,
        [FromQuery] int page = 1,
        CancellationToken cancellationToken = default)
    {
        var currentPage = GetPage<TreeNode>();

        if (currentPage is null)
        {
            return NotFound();
        }

        var currentAliasPath = currentPage.NodeAliasPath;
        var isRoot = currentAliasPath.Equals(BlogRootPath, StringComparison.OrdinalIgnoreCase);

        var term = Trim(q);
        var isSearch = term is { Length: >= 3 };
        var pageNumber = Math.Max(1, page);
        var oldestFirst = string.Equals(sort, "asc", StringComparison.OrdinalIgnoreCase);

        // Kentico tarafında MARS kapalı olduğundan DB kullanan servisleri paralel
        // çalıştırmıyoruz. Navigation verisi servis katmanında cache'lenmelidir.
        var navigationItems = await navigationService.GetAsync(Culture, cancellationToken);

        // Yalnızca kategori sayfalarında çalışır. İki küçük kategori sorgusu da
        // IPageRetriever cache'i ve doğru path dependency'leri ile saklanır.
        var categoryContext = !isRoot && !isSearch
            ? await GetCategoryContextAsync(currentAliasPath, Culture)
            : null;

        ArticlePageResult articlePage;

        if (isSearch)
        {
            articlePage = await SearchAsync(term!, pageNumber, cancellationToken);
        }
        else
        {
            articlePage = await ListAsync(
                currentAliasPath,
                pageNumber,
                oldestFirst,
                cancellationToken);
        }

        IReadOnlyList<ArticleCardDto> latest = [];

        if (isRoot && !isSearch)
        {
            latest = await articleService.GetLatestAsync(
                BlogRootPath,
                LatestCount,
                Culture,
                cancellationToken);
        }

        var nav = new BlogNavViewModel
        {
            Items = navigationItems,
            CurrentAliasPath = currentAliasPath,
        };

        var currentTitle = ValidationHelper.GetString(
            currentPage.GetValue(PageTitleField),
            currentPage.DocumentName);

        var pageTitle = isSearch
            ? $"“{term}” Arama Sonuçları"
            : categoryContext?.Title
              ?? (isRoot ? "Tüm Yazılar" : currentTitle);

        var model = new BlogPageViewModel
        {
            Title = pageTitle,
            Query = isSearch ? term : null,
            Nav = nav,
            AccentColor = ResolveAccentColor(nav),
            Latest = latest,
            OldestFirst = oldestFirst,
            Articles = articlePage.Items,
            PageNumber = articlePage.PageNumber,
            TotalPages = articlePage.TotalPages,
            TotalCount = articlePage.TotalCount,
            IsPartialCount = articlePage.IsPartialCount,
            BaseUrl = Request.Path.HasValue ? Request.Path.Value! : BlogRootPath,
            CategoryTabs = categoryContext?.Tabs ?? [],
        };

        return View(model);
    }

    private async Task<ArticlePageResult> ListAsync(
        string aliasPath,
        int pageNumber,
        bool oldestFirst,
        CancellationToken cancellationToken)
    {
        // Servisteki PathTypeEnum.Children sorgusu NestingLevel ile
        // sınırlandırılmadığı için bütün torunları kapsar. Böylece
        // /blog/gelecek "Tümü" sekmesi alt kategorilerdeki yazıları da,
        // /blog/gelecek/bilim ise yalnızca Bilim dalındaki yazıları getirir.
        var result = await articleService.GetPageAsync(
            aliasPath,
            pageNumber,
            PageSize,
            Culture,
            oldestFirst,
            cancellationToken);

        return new ArticlePageResult(
            result.Items,
            result.PageNumber,
            result.TotalPages,
            result.TotalCount,
            IsPartialCount: false);
    }

    private async Task<ArticlePageResult> SearchAsync(
        string term,
        int pageNumber,
        CancellationToken cancellationToken)
    {
        // Dış servis en fazla 100 kayıt döndürüyor. Sıra alaka puanıdır ve
        // Article servisinden dönen DTO'lar aynı alias-path sırasına dizilir.
        var search = await siteSearch.SearchAsync(
            new SiteSearchQuery(term, 1, Culture, 100, SearchScope.Blog),
            cancellationToken);

        if (search.Hits.Count == 0)
        {
            return new ArticlePageResult([], 1, 1, 0, search.IsPartialCount);
        }

        var aliasPaths = search.Hits
            .Select(hit => ToAliasPath(hit.Url))
            .Where(path => path is not null)
            .Select(path => path!)
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        var ordered = await articleService.GetByAliasPathsAsync(
            aliasPaths,
            Culture,
            cancellationToken);

        var totalCount = ordered.Count;
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)PageSize));
        var safePage = Math.Min(pageNumber, totalPages);
        var items = ordered
            .Skip((safePage - 1) * PageSize)
            .Take(PageSize)
            .ToList();

        return new ArticlePageResult(
            items,
            safePage,
            totalPages,
            totalCount,
            search.IsPartialCount);
    }

    private static string? ToAliasPath(string url)
    {
        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            return null;
        }

        var path = uri.AbsolutePath.TrimEnd('/');

        return string.IsNullOrEmpty(path) ? null : path;
    }

    private static string? Trim(string? query)
    {
        if (string.IsNullOrWhiteSpace(query))
        {
            return null;
        }

        var term = query.Trim();

        return term.Length > MaxQueryLength ? term[..MaxQueryLength] : term;
    }

    private async Task<CategoryContext?> GetCategoryContextAsync(
        string currentAliasPath,
        string culture)
    {
        var topCategoryPath = GetTopCategoryPath(currentAliasPath);

        if (topCategoryPath is null)
        {
            return null;
        }

        var siteName = SiteContext.CurrentSiteName;
        var cachePrefix = $"blog|category-tabs|{siteName}|{culture}|{topCategoryPath}"
            .ToLowerInvariant();

        // Üst kategoriyi ayrı alıyoruz. Böylece alt kategorideyken de sayfa
        // başlığı "Bilim" yerine "Gelecek" olarak kalır.
        var topCategory = (await pageRetriever.RetrieveAsync<CmsBlogCategory>(
                query => query
                    .Path(topCategoryPath, PathTypeEnum.Single)
                    .Columns(
                        nameof(TreeNode.NodeID),
                        nameof(TreeNode.NodeSiteID),
                        nameof(TreeNode.NodeAliasPath),
                        nameof(TreeNode.DocumentName),
                        PageTitleField)
                    .WithPageUrlPaths(),
                cache => cache
                    .Key($"{cachePrefix}|root")
                    .Dependencies((_, builder) =>
                        builder.PagePath(topCategoryPath, PathTypeEnum.Single))))
            .FirstOrDefault();

        if (topCategory is null)
        {
            return null;
        }

        // PathTypeEnum.Children + NestingLevel(1), makale torunlarına inmeden
        // yalnızca Bilim / İnovasyon / Teknoloji gibi doğrudan çocukları alır.
        var children = (await pageRetriever.RetrieveAsync<CmsBlogCategory>(
                query => query
                    .Path(topCategoryPath, PathTypeEnum.Children)
                    .NestingLevel(1)
                    .Columns(
                        nameof(TreeNode.NodeID),
                        nameof(TreeNode.NodeSiteID),
                        nameof(TreeNode.NodeAliasPath),
                        nameof(TreeNode.NodeOrder),
                        nameof(TreeNode.DocumentName),
                        PageTitleField)
                    .WithPageUrlPaths()
                    .OrderByAscending(nameof(TreeNode.NodeOrder)),
                cache => cache
                    .Key($"{cachePrefix}|children")
                    .Dependencies((_, builder) =>
                        builder.PagePath(topCategoryPath, PathTypeEnum.Children))))
            .ToList();

        var tabs = new List<BlogCategoryTabViewModel>(children.Count + 1)
        {
            new()
            {
                Title = "Tümü",
                AliasPath = topCategory.NodeAliasPath,
                Url = pageUrlRetriever.Retrieve(topCategory).RelativePath,
                IsActive = IsSamePath(currentAliasPath, topCategory.NodeAliasPath),
            },
        };

        tabs.AddRange(children.Select(child => new BlogCategoryTabViewModel
        {
            Title = ValidationHelper.GetString(
                child.GetValue(PageTitleField),
                child.DocumentName),
            AliasPath = child.NodeAliasPath,
            Url = pageUrlRetriever.Retrieve(child).RelativePath,
            IsActive = IsSamePathOrDescendant(currentAliasPath, child.NodeAliasPath),
        }));

        var title = ValidationHelper.GetString(
            topCategory.GetValue(PageTitleField),
            topCategory.DocumentName);

        return new CategoryContext(title, tabs);
    }

    private static string? GetTopCategoryPath(string aliasPath)
    {
        var normalized = aliasPath.TrimEnd('/');
        var prefix = BlogRootPath + "/";

        if (!normalized.StartsWith(prefix, StringComparison.OrdinalIgnoreCase))
        {
            return null;
        }

        var relativePath = normalized[prefix.Length..];
        var separatorIndex = relativePath.IndexOf('/');
        var topSegment = separatorIndex < 0
            ? relativePath
            : relativePath[..separatorIndex];

        return string.IsNullOrWhiteSpace(topSegment)
            ? null
            : $"{BlogRootPath}/{topSegment}";
    }

    private static bool IsSamePath(string left, string right) =>
        left.TrimEnd('/').Equals(
            right.TrimEnd('/'),
            StringComparison.OrdinalIgnoreCase);

    private static bool IsSamePathOrDescendant(string path, string parentPath)
    {
        var normalizedPath = path.TrimEnd('/');
        var normalizedParent = parentPath.TrimEnd('/');

        return normalizedPath.Equals(
                   normalizedParent,
                   StringComparison.OrdinalIgnoreCase)
               || normalizedPath.StartsWith(
                   normalizedParent + "/",
                   StringComparison.OrdinalIgnoreCase);
    }

    private static string ResolveAccentColor(BlogNavViewModel nav) =>
        nav.Items.FirstOrDefault(nav.IsActive)?.Color ?? "blue";

    private sealed record ArticlePageResult(
        IReadOnlyList<ArticleCardDto> Items,
        int PageNumber,
        int TotalPages,
        int TotalCount,
        bool IsPartialCount);

    private sealed record CategoryContext(
        string Title,
        IReadOnlyList<BlogCategoryTabViewModel> Tabs);
}
