using CMS.Core;
using CMS.DocumentEngine;
using CMS.Helpers;
using Kentico.Content.Web.Mvc;
using YkbYapikredi.Application.Blog;
using YkbYapikredi.Infrastructure.Integration;
using CmsBlogArticle = CMS.DocumentEngine.Types.YkbYapikredi.YkbBlogArticle;

namespace YkbYapikredi.Infrastructure.Blog;

/// <summary>
/// Yazı listelerini içerik ağacından getirir. Listeleme, slider ve arama kartı
/// sorgularının sayısı kart sayısından bağımsızdır.
/// </summary>
public sealed class BlogArticleService : IBlogArticleService
{
    private static readonly string ArticleClassName = CmsBlogArticle.CLASS_NAME;

    private const string ShowOnSliderColumn = "ShowOnSlider";
    private const string PublishFromColumn = "DocumentPublishFrom";
    private const string RecommendedColumn = "IsRecommended";
    private const string BlogRootPath = "/blog";
    private const string EventSource = "Blog";

    private readonly IPageRetriever pageRetriever;
    private readonly IPageUrlRetriever pageUrlRetriever;
    private readonly IEventLogService eventLog;

    public BlogArticleService(
        IPageRetriever pageRetriever,
        IPageUrlRetriever pageUrlRetriever,
        IEventLogService eventLog)
    {
        this.pageRetriever = pageRetriever;
        this.pageUrlRetriever = pageUrlRetriever;
        this.eventLog = eventLog;
    }

    public async Task<ArticleListResult> GetPageAsync(
        string path,
        int pageNumber,
        int pageSize,
        string culture,
        bool oldestFirst = false,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || pageSize <= 0)
        {
            return new ArticleListResult();
        }

        // Kentico istek başına tek DB bağlantısı kullanıyor ve MARS kapalı.
        // Sayım ve sayfa sorgusunu paralel çalıştırmıyoruz.
        var totalCount = await CountAsync(path, culture, cancellationToken);
        var totalPages = Math.Max(1, (int)Math.Ceiling(totalCount / (double)pageSize));
        var safePage = Math.Clamp(pageNumber, 1, totalPages);

        var nodes = await pageRetriever.RetrieveMultipleAsync(
            query => ApplyListFilters(query, path, culture, oldestFirst)
                .Page(safePage - 1, pageSize)
                .WithPageUrlPaths()
                .WithCoupledColumns(),
            cancellationToken: cancellationToken);

        return new ArticleListResult
        {
            Items = [.. nodes.Select(Map)],
            TotalCount = totalCount,
            PageNumber = safePage,
            TotalPages = totalPages,
        };
    }

    public async Task<IReadOnlyList<ArticleCardDto>> GetLatestAsync(
        string path,
        int count,
        string culture,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || count <= 0)
        {
            return [];
        }

        var nodes = await pageRetriever.RetrieveMultipleAsync(
            query => ApplyListFilters(
                    query,
                    path,
                    culture,
                    oldestFirst: false,
                    showOnSliderOnly: true)
                .TopN(count)
                .WithPageUrlPaths()
                .WithCoupledColumns(),
            cancellationToken: cancellationToken);

        return [.. nodes.Select(Map)];
    }

    public async Task<IReadOnlyList<ArticleCardDto>> GetByAliasPathsAsync(
        IReadOnlyList<string> aliasPaths,
        string culture,
        CancellationToken cancellationToken = default)
    {
        if (aliasPaths.Count == 0)
        {
            return [];
        }

        var paths = aliasPaths
            .Select(NormalizeAliasPath)
            .Where(path => path is not null)
            .Select(path => path!)
            .Where(path => IsSamePathOrDescendant(path, BlogRootPath))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToList();

        if (paths.Count == 0)
        {
            return [];
        }

        var nodes = await pageRetriever.RetrieveMultipleAsync(
            query => query
                .Path(BlogRootPath, PathTypeEnum.Children)
                .Types(ArticleClassName)
                .Culture(culture)
                .CombineWithDefaultCulture(false)
                .WhereIn(nameof(TreeNode.NodeAliasPath), paths)
                .WithPageUrlPaths()
                .WithCoupledColumns(),
            cancellationToken: cancellationToken);

        var byPath = new Dictionary<string, TreeNode>(StringComparer.OrdinalIgnoreCase);

        foreach (var node in nodes)
        {
            byPath.TryAdd(node.NodeAliasPath.TrimEnd('/'), node);
        }

        var cards = new List<ArticleCardDto>(paths.Count);
        var seen = new HashSet<int>();

        // Arama servisinin alaka sırasını koruyoruz.
        foreach (var path in paths)
        {
            if (!byPath.TryGetValue(path, out var node))
            {
                continue;
            }

            if (seen.Add(node.NodeID))
            {
                cards.Add(Map(node));
            }
        }

        if (cards.Count == 0)
        {
            ThrottledEventLog.LogError(
                eventLog,
                EventSource,
                "SEARCHMAPPING",
                $"Arama {aliasPaths.Count} sonuç döndürdü ancak içerik ağacında " +
                $"karşılığı bulunamadı. Servisten gelen ilk yol: '{paths[0]}'. " +
                "NodeAliasPath değerleriyle karşılaştırınız.");
        }

        return cards;
    }

    public async Task<IReadOnlyList<ArticleCardDto>> GetRelatedAsync(
        string path,
        string excludeAliasPath,
        int count,
        string culture,
        string? fallbackPath = null,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(path) || count <= 0)
        {
            return [];
        }

        var items = await TakeLatestAsync(
            path,
            [excludeAliasPath],
            count,
            culture,
            cancellationToken);

        if (items.Count < count
            && !string.IsNullOrWhiteSpace(fallbackPath)
            && !string.Equals(fallbackPath, path, StringComparison.OrdinalIgnoreCase))
        {
            var exclude = new List<string> { excludeAliasPath };
            exclude.AddRange(items.Select(item => item.AliasPath));

            var extra = await TakeLatestAsync(
                fallbackPath,
                exclude,
                count - items.Count,
                culture,
                cancellationToken);

            items = [.. items, .. extra];
        }

        return items;
    }

    private async Task<IReadOnlyList<ArticleCardDto>> TakeLatestAsync(
        string path,
        IReadOnlyCollection<string> excludeAliasPaths,
        int count,
        string culture,
        CancellationToken cancellationToken)
    {
        if (count <= 0)
        {
            return [];
        }

        var nodes = await pageRetriever.RetrieveMultipleAsync(
            query => ApplyListFilters(query, path, culture, recommendedFirst: true)
                .TopN(count + excludeAliasPaths.Count)
                .WithPageUrlPaths()
                .WithCoupledColumns(),
            cancellationToken: cancellationToken);

        return
        [
            .. nodes
                .Where(node => !excludeAliasPaths.Contains(
                    node.NodeAliasPath,
                    StringComparer.OrdinalIgnoreCase))
                .Take(count)
                .Select(Map)
        ];
    }

    /// <summary>
    /// Derinlik sınırı yoktur. Children, verilen yolun altındaki tüm seviyeleri
    /// kapsar; yalnızca makale page type'ı seçilir.
    /// </summary>
    private static MultiDocumentQuery ApplyListFilters(
        MultiDocumentQuery query,
        string path,
        string culture,
        bool oldestFirst = false,
        bool recommendedFirst = false,
        bool showOnSliderOnly = false)
    {
        query = query
            .Path(path, PathTypeEnum.Children)
            .Types(ArticleClassName)
            .Culture(culture)
            .CombineWithDefaultCulture(false);

        if (showOnSliderOnly)
        {
            query = query.WhereEquals(ShowOnSliderColumn, true);
        }

        if (recommendedFirst)
        {
            query = query.OrderByDescending(RecommendedColumn);
        }

        return oldestFirst
            ? query.OrderBy(PublishFromColumn)
            : query.OrderByDescending(PublishFromColumn);
    }

    private async Task<int> CountAsync(
        string path,
        string culture,
        CancellationToken cancellationToken)
    {
        // Yalnızca NodeID çekilir; coupled tablo join'i açılmaz.
        var nodes = await pageRetriever.RetrieveMultipleAsync(
            query =>
            {
                ApplyListFilters(query, path, culture);
                query.Columns(nameof(TreeNode.NodeID));
            },
            cancellationToken: cancellationToken);

        return nodes.Count();
    }

    private ArticleCardDto Map(TreeNode node) => new()
    {
        Title = GetString(node, "PageTitle", node.DocumentName),
        Summary = GetString(node, "Summary", string.Empty),
        Url = TryGetUrl(node) ?? "#",
        ImageUrl = NullIfBlank(GetString(node, "CoverImage", string.Empty)),
        AliasPath = node.NodeAliasPath,
        PublishDate = GetPublishDate(node),
        ReadingMinutes = ValidationHelper.GetInteger(node.GetValue("ReadingTime"), 0),
    };

    private static DateTime GetPublishDate(TreeNode node)
    {
        var publishFrom = ValidationHelper.GetDateTime(
            node.GetValue(PublishFromColumn),
            DateTime.MinValue);

        return publishFrom == DateTime.MinValue
            ? node.DocumentCreatedWhen
            : publishFrom;
    }

    private static string GetString(TreeNode node, string fieldName, string fallback)
    {
        var value = ValidationHelper.GetString(
            node.GetValue(fieldName),
            string.Empty);

        return string.IsNullOrWhiteSpace(value) ? fallback : value;
    }

    private static string? NormalizeAliasPath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            return null;
        }

        var normalized = "/" + path.Trim().Trim('/');

        return normalized.Length == 1 ? null : normalized;
    }

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

    private static string? NullIfBlank(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value;

    private string? TryGetUrl(TreeNode node)
    {
        try
        {
            return pageUrlRetriever.Retrieve(node)?.RelativePath;
        }
        catch (InvalidOperationException)
        {
            return null;
        }
    }
}
