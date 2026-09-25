namespace YkbYapikredi.Web.Models;

/// <summary>
/// Üst kategori başlığının altında gösterilen "Tümü / İnovasyon / Teknoloji / Bilim"
/// sekmelerinden biri. Veriler controller'daki cache'li Kentico sorgusundan gelir.
/// </summary>
public sealed class BlogCategoryTabViewModel
{
    public string Title { get; init; } = string.Empty;

    public string AliasPath { get; init; } = string.Empty;

    public string Url { get; init; } = string.Empty;

    public bool IsActive { get; init; }
}
