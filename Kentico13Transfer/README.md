# Kentico 13 aktarım paketi

Bu klasör mock projede derlenmez ve yayınlanmaz. Dosyalar gerçek Kentico 13
projesindeki karşılıklarının yerine kopyalanmak üzere hazırlanmıştır.

## Kopyalanacak dosyalar

1. `Controllers/BlogController.cs`
2. `Models/BlogPageViewModel.cs`
3. `Models/BlogCategoryTabViewModel.cs`
4. `Services/BlogArticleService.cs`
5. `Views/Blog/Partials/_BlogCategoryTabs.cshtml`
6. `css/blog-category-tabs.css`

`blog-category-tabs.css` dosyasını gerçek projedeki CSS minify/bundle girdilerine
ekleyin. Üretilen `blog.min.css` dosyasını elle düzenlemeyin.

## Index.cshtml eklemesi

Kategori sekmeleri, `blog-section-head` bloğının kapanışından hemen sonra ve
`blog-cards` listesinden önce render edilmelidir:

```cshtml
@await Html.PartialAsync(
    "~/Views/Blog/Partials/_BlogCategoryTabs.cshtml",
    Model)
```

## İçerik ağacı ve çalışma şekli

Beklenen yapı:

```text
/blog
  /gelecek                 (YkbBlogCategory)
    /bilim                 (YkbBlogCategory)
      /article-1           (makale page type)
    /inovasyon             (YkbBlogCategory)
      /article-2
    /teknoloji             (YkbBlogCategory)
      /article-3
```

- `/blog/gelecek`: başlık `Gelecek`, aktif sekme `Tümü`, bütün alt dallardaki
  makaleler.
- `/blog/gelecek/bilim`: başlık yine `Gelecek`, aktif sekme `Bilim`, yalnızca
  Bilim dalındaki makaleler.
- Sekme sırası Kentico içerik ağacındaki `NodeOrder` sırasıdır. Görseldeki sıra
  isteniyorsa alt sayfaları Pages uygulamasında o sıraya taşıyın.

## Performans notları

- Controller alt kategori listesini `IPageRetriever` ile yalnızca gerekli
  kolonları seçerek alır.
- `WithPageUrlPaths()` URL üretirken ek sorguları önler.
- Üst kategori ve doğrudan çocuk sorguları culture + site + path içeren ayrı
  cache key'lerine ve `PagePath` dependency'lerine sahiptir. Kategori eklenince,
  silinince veya değişince cache otomatik temizlenir.
- Navigation, kategori ve makale çağrıları MARS kapalı bağlantılara uygun olarak
  sırayla çalışır.
- Kart kategorisi `Nav.FindCategory` ile bellekte bulunur; kart başına DB sorgusu
  yoktur.

## BlogArticleService davranışı

Mevcut servisteki `Path(aliasPath, PathTypeEnum.Children)` kullanımı doğrudur.
`NestingLevel` verilmediği için sorgu yalnızca doğrudan çocuklarla sınırlanmaz;
bütün alt seviyelerdeki makaleleri kapsar. Bu nedenle `/blog/gelecek` sorgusu
Bilim, İnovasyon ve Teknoloji altındaki makaleleri birlikte getirir.

Transfer paketindeki son servis ayrıca:

- Sayfalama için Kentico `Page` metodunu kullanır; tüm makaleleri belleğe almaz.
- Kart getirilen bütün sorgulara `WithPageUrlPaths()` ekler. Böylece `Map`
  içindeki URL üretimi kart başına ek sorgu açmaz.
- Arama sonuçlarını sabit `NodeLevel = 4` ve `NodeName` ile değil, doğrudan
  `NodeAliasPath` ile eşler. Aynı isimli iki makale veya ağaç derinliği değişimi
  yanlış kart döndürmez.
- Kentico sorgularını MARS kısıtına uygun biçimde sıralı çalıştırır.

## Varsayımlar

- Üst ve alt kategoriler aynı generated page type'ı kullanır:
  `YkbYapikredi.YkbBlogCategory`.
- Kategori başlığı alanının kod adı `PageTitle`dır; boşsa `DocumentName`
  kullanılır.
- Alt kategori seviyesi bir tanedir. Daha derin kategori ağacı gerekiyorsa
  sekme seçme kuralı ayrıca genişletilmelidir.
- `IBlogNavigationService` ve `IBlogArticleService` mevcut projedeki imzalarını
  korur; yeni bir DI kaydı gerekmez. `IPageRetriever` ve `IPageUrlRetriever`
  Kentico tarafından zaten kaydedilir.
