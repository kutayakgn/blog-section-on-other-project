using System.Text;
using Microsoft.AspNetCore.Mvc;

namespace YkbYapikredi.Web.Controllers;

[ApiController]
public sealed class MockMediaController : ControllerBase
{
    private static readonly string[] Palette = ["#004990", "#6862af", "#ff4b4b", "#9fb21b", "#fcb636", "#ada5ff"];

    [HttpGet("/mock/blog/{slug}.svg")]
    public IActionResult Article(string slug)
    {
        var color = Palette[Math.Abs(StringComparer.Ordinal.GetHashCode(slug)) % Palette.Length];
        var svg = $$"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1200" height="675" viewBox="0 0 1200 675">
              <defs>
                <linearGradient id="g" x1="0" y1="0" x2="1" y2="1">
                  <stop stop-color="{{color}}"/>
                  <stop offset="1" stop-color="#102a43"/>
                </linearGradient>
              </defs>
              <rect width="1200" height="675" fill="url(#g)"/>
              <circle cx="1040" cy="90" r="240" fill="#fff" opacity=".08"/>
              <circle cx="1030" cy="620" r="320" fill="#fff" opacity=".06"/>
              <path d="M0 520 C260 430 420 650 720 545 C930 470 1050 500 1200 430 V675 H0Z" fill="#fff" opacity=".06"/>
            </svg>
            """;

        return Svg(svg);
    }

    [HttpGet("/assets/images/blog/ykb-blog-logo.png")]
    public IActionResult HeaderLogo() => Svg("""
        <svg xmlns="http://www.w3.org/2000/svg" width="360" height="60" viewBox="0 0 360 60">
          <g fill="#004990"><rect x="1" y="5" width="10" height="50" rx="5"/><rect x="18" y="5" width="10" height="50" rx="5"/><path d="M36 5h11l14 20L75 5h11L66 34v21H56V34z"/></g>
          <text x="100" y="41" fill="#172b4d" font-family="Segoe UI,Arial,sans-serif" font-size="31" font-weight="600">Yapı Kredi Blog</text>
        </svg>
        """);

    [HttpGet("/blog/img/{fileName}")]
    public IActionResult BlogImage(string fileName)
    {
        if (fileName.Equals("footer-logo.png", StringComparison.OrdinalIgnoreCase))
        {
            return Svg("""
                <svg xmlns="http://www.w3.org/2000/svg" width="366" height="64" viewBox="0 0 366 64">
                  <text x="0" y="44" fill="#fff" font-family="Segoe UI,Arial,sans-serif" font-size="38" font-weight="600">Yapı Kredi</text>
                </svg>
                """);
        }

        var color = fileName.ToLowerInvariant() switch
        {
            var name when name.Contains("red") => "#fff4f4",
            var name when name.Contains("orange") => "#fff9ec",
            var name when name.Contains("lilac") => "#f8f6ff",
            var name when name.Contains("purple") => "#f4f3ff",
            var name when name.Contains("green") => "#f7f9ed",
            _ => "#f3f8fc"
        };

        return Svg($$"""
            <svg xmlns="http://www.w3.org/2000/svg" width="1600" height="900" viewBox="0 0 1600 900">
              <rect width="1600" height="900" fill="{{color}}"/>
              <circle cx="1480" cy="30" r="360" fill="#fff" opacity=".72"/>
              <circle cx="100" cy="820" r="280" fill="#fff" opacity=".46"/>
            </svg>
            """);
    }

    private FileContentResult Svg(string value) =>
        File(Encoding.UTF8.GetBytes(value), "image/svg+xml; charset=utf-8");

}
