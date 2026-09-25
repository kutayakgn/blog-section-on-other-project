using System.Globalization;
using YkbYapikredi.Web.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();
builder.Services.AddSingleton<IBlogService, MockBlogService>();
builder.Services.AddSingleton<IAssetBundleService, AssetBundleService>();

var app = builder.Build();

var turkishCulture = CultureInfo.GetCultureInfo("tr-TR");
CultureInfo.DefaultThreadCurrentCulture = turkishCulture;
CultureInfo.DefaultThreadCurrentUICulture = turkishCulture;

app.MapControllers();
app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Blog}/{action=Index}/{id?}");

app.MapGet("/", () => Results.Redirect("/blog"));

app.Run();
