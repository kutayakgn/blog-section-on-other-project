using Microsoft.AspNetCore.Mvc;
using YkbYapikredi.Web.Models;
using YkbYapikredi.Web.Services;

namespace YkbYapikredi.Web.Controllers;

[Route("blog")]
public sealed class BlogController(IBlogService blogService) : Controller
{
    [HttpGet("")]
    public IActionResult Index(string? q, int page = 1, string? sort = null)
    {
        var model = blogService.GetPage(new BlogQuery(null, q, page, IsOldest(sort)));
        return View(model);
    }

    [HttpGet("kategori/{categorySlug}")]
    public IActionResult Category(string categorySlug, int page = 1, string? sort = null)
    {
        if (blogService.FindCategory(categorySlug) is null)
        {
            return NotFound();
        }

        var model = blogService.GetPage(new BlogQuery(categorySlug, null, page, IsOldest(sort)));
        return View("Index", model);
    }

    private static bool IsOldest(string? sort) =>
        string.Equals(sort, "oldest", StringComparison.OrdinalIgnoreCase);
}
