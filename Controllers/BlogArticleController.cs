using Microsoft.AspNetCore.Mvc;
using YkbYapikredi.Web.Services;

namespace YkbYapikredi.Web.Controllers;

[Route("blog/yazi")]
public sealed class BlogArticleController(IBlogService blogService) : Controller
{
    [HttpGet("{slug}")]
    public IActionResult Index(string slug)
    {
        var absoluteUrl = $"{Request.Scheme}://{Request.Host}{Request.PathBase}{Request.Path}";
        var model = blogService.GetArticle(slug, absoluteUrl);

        if (model is null)
        {
            return NotFound();
        }

        ViewData["Title"] = model.Title;
        return View(model);
    }
}
