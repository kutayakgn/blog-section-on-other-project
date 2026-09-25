namespace YkbYapikredi.Application.Blog
{
    public sealed class BlogArticle
    {
        public required string Slug { get; init; }
        public required string Title { get; init; }
        public required string Summary { get; init; }
        public required string Content { get; init; }
        public required string CategorySlug { get; init; }
        public required string ImageUrl { get; init; }
        public required DateTime PublishDate { get; init; }
        public required int ReadingMinutes { get; init; }
        public string Url => $"/blog/yazi/{Slug}";
    }
}

namespace YkbYapikredi.Web.Models
{
    using YkbYapikredi.Application.Blog;

    public sealed class BlogCategory
    {
        public required string Slug { get; init; }
        public required string Title { get; init; }
        public required string Color { get; init; }
        public required string IconCssClass { get; init; }
        public string Url => string.IsNullOrEmpty(Slug) ? "/blog" : $"/blog/kategori/{Slug}";
    }

    public sealed class BlogNavViewModel
    {
        public required IReadOnlyList<BlogCategory> Items { get; init; }
        public string? ActiveSlug { get; init; }

        public bool IsActive(BlogCategory item) =>
            string.Equals(item.Slug, ActiveSlug ?? string.Empty, StringComparison.OrdinalIgnoreCase);
    }

    public class BlogLayoutViewModel
    {
        public string BaseUrl { get; init; } = "/blog";
        public string? Query { get; init; }
        public string AccentColor { get; init; } = "blue";
        public required BlogNavViewModel Nav { get; init; }
    }

    public sealed class BlogPageViewModel : BlogLayoutViewModel
    {
        public required string Title { get; init; }
        public required IReadOnlyList<BlogArticle> Articles { get; init; }
        public required IReadOnlyList<BlogArticle> Latest { get; init; }
        public required IReadOnlyDictionary<string, BlogCategory> Categories { get; init; }
        public required string PageBaseUrl { get; init; }
        public required string SortToggleUrl { get; init; }
        public int PageNumber { get; init; }
        public int TotalPages { get; init; }
        public int TotalCount { get; init; }
        public bool IsSearch { get; init; }
        public bool IsPartialCount { get; init; }
        public bool OldestFirst { get; init; }

        public BlogCategory? GetCategory(BlogArticle article) =>
            Categories.GetValueOrDefault(article.CategorySlug);

        public string PageUrl(int number)
        {
            var queryParts = new List<string>();

            if (!string.IsNullOrWhiteSpace(Query))
            {
                queryParts.Add($"q={Uri.EscapeDataString(Query)}");
            }

            if (OldestFirst)
            {
                queryParts.Add("sort=oldest");
            }

            if (number > 1)
            {
                queryParts.Add($"page={number}");
            }

            return queryParts.Count == 0
                ? PageBaseUrl
                : $"{PageBaseUrl}?{string.Join('&', queryParts)}";
        }
    }

    public sealed class BlogCardViewModel
    {
        public required BlogArticle Article { get; init; }
        public BlogCategory? Category { get; init; }
    }

    public sealed class BlogArticleViewModel : BlogLayoutViewModel
    {
        public required string Title { get; init; }
        public required string Summary { get; init; }
        public required string Content { get; init; }
        public required string CoverImageUrl { get; init; }
        public required string AbsoluteUrl { get; init; }
        public required DateTime PublishDate { get; init; }
        public required int ReadingMinutes { get; init; }
        public BlogCategory? Category { get; init; }
        public BlogEmbedViewModel? Embed { get; init; }
        public required IReadOnlyList<BlogBreadcrumbViewModel> Breadcrumb { get; init; }
        public required IReadOnlyList<BlogArticle> Related { get; init; }
        public required IReadOnlyDictionary<string, BlogCategory> Categories { get; init; }

        public BlogCategory? GetCategory(BlogArticle article) =>
            Categories.GetValueOrDefault(article.CategorySlug);
    }

    public sealed class BlogBreadcrumbViewModel
    {
        public required string Title { get; init; }
        public string? Url { get; init; }
    }

    public sealed class BlogEmbedViewModel
    {
        public required string Url { get; init; }
        public required string Title { get; init; }
        public int? FixedHeight { get; init; }
    }

    public sealed record BlogQuery(string? CategorySlug, string? Query, int Page, bool OldestFirst);
}
