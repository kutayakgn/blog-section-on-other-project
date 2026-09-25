using Microsoft.AspNetCore.Razor.TagHelpers;

namespace YkbYapikredi.Web.Helpers;

[HtmlTargetElement("i", Attributes = "class")]
public sealed class BlogIconTagHelper : TagHelper
{
    [HtmlAttributeName("class")]
    public string CssClass { get; set; } = string.Empty;

    public override void Process(TagHelperContext context, TagHelperOutput output)
    {
        var iconName = CssClass
            .Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .FirstOrDefault(name => name.StartsWith("icon-", StringComparison.Ordinal));

        if (iconName is null)
        {
            return;
        }

        output.TagName = "svg";
        output.TagMode = TagMode.StartTagAndEndTag;
        output.Attributes.SetAttribute("class", "blog-nav__svg-icon");
        output.Attributes.SetAttribute("viewBox", "0 0 24 24");
        output.Attributes.SetAttribute("width", "22");
        output.Attributes.SetAttribute("height", "22");
        output.Attributes.SetAttribute("fill", "none");
        output.Attributes.SetAttribute("stroke", "currentColor");
        output.Attributes.SetAttribute("stroke-width", "1.8");
        output.Attributes.SetAttribute("stroke-linecap", "round");
        output.Attributes.SetAttribute("stroke-linejoin", "round");
        output.Content.SetHtmlContent(GetIcon(iconName));
    }

    private static string GetIcon(string iconName) => iconName switch
    {
        "icon-People" => "<circle cx=\"9\" cy=\"8\" r=\"3\"/><path d=\"M3.5 19v-2.2A4.8 4.8 0 0 1 8.3 12h1.4a4.8 4.8 0 0 1 4.8 4.8V19\"/><path d=\"M15 5.5a3 3 0 0 1 0 5.8M17 12.5a4.5 4.5 0 0 1 3.5 4.3V19\"/>",
        "icon-Laptop" => "<rect x=\"4\" y=\"5\" width=\"16\" height=\"11\" rx=\"1.5\"/><path d=\"M2.5 19h19M9 19h6\"/>",
        "icon-Chart" => "<path d=\"M4 19V9M10 19V5M16 19v-7M22 19H2\"/>",
        "icon-Tree" => "<path d=\"M12 21v-7M8.5 17.5 12 14l3.5 3.5\"/><path d=\"M6 13.5a4 4 0 0 1 2.2-7.6A4.5 4.5 0 0 1 17 7.2a3.5 3.5 0 0 1 .5 6.8H6Z\"/>",
        "icon-Building" => "<path d=\"M4 21V5l8-3 8 3v16M2 21h20\"/><path d=\"M8 7h1M15 7h1M8 11h1M15 11h1M8 15h1M15 15h1M10 21v-3h4v3\"/>",
        _ => "<path d=\"M5 4.5A2.5 2.5 0 0 1 7.5 2H12v18H7.5A2.5 2.5 0 0 0 5 22V4.5Z\"/><path d=\"M19 4.5A2.5 2.5 0 0 0 16.5 2H12v18h4.5A2.5 2.5 0 0 1 19 22V4.5Z\"/>"
    };
}
