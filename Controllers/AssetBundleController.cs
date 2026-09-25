using Microsoft.AspNetCore.Mvc;
using YkbYapikredi.Web.Services;

namespace YkbYapikredi.Web.Controllers;

[ApiController]
public sealed class AssetBundleController(IAssetBundleService assetBundleService) : ControllerBase
{
    [HttpGet("/css/blog.min.css")]
    public async Task<IActionResult> Css() =>
        Content(await assetBundleService.GetCssAsync(), "text/css; charset=utf-8");

    [HttpGet("/js/blog.min.js")]
    public async Task<IActionResult> JavaScript() =>
        Content(await assetBundleService.GetJavaScriptAsync(), "text/javascript; charset=utf-8");
}
