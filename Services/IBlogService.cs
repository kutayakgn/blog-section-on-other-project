using YkbYapikredi.Web.Models;

namespace YkbYapikredi.Web.Services;

public interface IBlogService
{
    IReadOnlyList<BlogCategory> Categories { get; }
    BlogCategory? FindCategory(string slug);
    BlogPageViewModel GetPage(BlogQuery query);
    BlogArticleViewModel? GetArticle(string slug, string absoluteUrl);
}
