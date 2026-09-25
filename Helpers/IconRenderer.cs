using Microsoft.AspNetCore.Html;

namespace YkbYapikredi.Web.Helpers;

public static class IconRenderer
{
    public static IHtmlContent Render(string iconName)
    {
        var svg = iconName switch
        {
            "icon-angle-left" => Arrow("M15 18l-6-6 6-6"),
            "icon-angle-right" => Arrow("M9 18l6-6-6-6"),
            _ => """
                <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" aria-hidden="true">
                    <circle cx="11" cy="11" r="7"></circle>
                    <path d="m20 20-4-4"></path>
                </svg>
                """
        };

        return new HtmlString(svg);
    }

    private static string Arrow(string path) => $$"""
        <svg viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">
            <path d="{{path}}"></path>
        </svg>
        """;
}
