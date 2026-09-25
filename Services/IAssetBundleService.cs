namespace YkbYapikredi.Web.Services;

public interface IAssetBundleService
{
    Task<string> GetCssAsync();
    Task<string> GetJavaScriptAsync();
}
