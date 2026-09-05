using System.IO;
using System.Net.Http;
using System.Text.Json;

namespace LoLSkinManager.App;

public sealed class SkinPackageRepositoryService
{
    private const string RawBaseUrl = "https://raw.githubusercontent.com/MMcf09/LoLSkinManager/main/";
    private const string IndexUrl = RawBaseUrl + "skins/index.json";

    private readonly HttpClient _http = new();
    private List<RepositorySkinPackage>? _cache;

    public async Task<RepositorySkinPackage?> FindPackageAsync(SkinCatalogItem skin)
    {
        await EnsureIndexAsync();
        return _cache!.FirstOrDefault(item =>
            item.ChampionId.Equals(skin.ChampionId, StringComparison.OrdinalIgnoreCase)
            && item.SkinNumber == skin.Number);
    }

    public async Task<string> DownloadPackageAsync(RepositorySkinPackage package)
    {
        if (string.IsNullOrWhiteSpace(package.File) ||
            !package.File.EndsWith(".fantome", StringComparison.OrdinalIgnoreCase))
            throw new InvalidOperationException("A entrada do catálogo não aponta para um arquivo .fantome válido.");

        var relativePath = package.File.TrimStart('/').Replace('\\', '/');
        if (relativePath.Contains("..", StringComparison.Ordinal))
            throw new InvalidOperationException("Caminho de pacote inválido no índice.");

        var url = RawBaseUrl + relativePath;
        var bytes = await _http.GetByteArrayAsync(url);

        var tempDirectory = Path.Combine(Path.GetTempPath(), "LoLSkinManager", "RepositoryDownloads");
        Directory.CreateDirectory(tempDirectory);

        var safeFileName = Path.GetFileName(relativePath);
        var destination = Path.Combine(tempDirectory, $"{Guid.NewGuid():N}_{safeFileName}");
        await File.WriteAllBytesAsync(destination, bytes);
        return destination;
    }

    public void InvalidateCache() => _cache = null;

    private async Task EnsureIndexAsync()
    {
        if (_cache != null)
            return;

        try
        {
            var json = await _http.GetStringAsync(IndexUrl);
            _cache = JsonSerializer.Deserialize<List<RepositorySkinPackage>>(json,
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true })
                ?? new List<RepositorySkinPackage>();
        }
        catch (HttpRequestException ex)
        {
            throw new InvalidOperationException("Não foi possível carregar skins/index.json do GitHub.", ex);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("O arquivo skins/index.json está inválido.", ex);
        }
    }
}

public sealed class RepositorySkinPackage
{
    public string ChampionId { get; set; } = string.Empty;
    public int SkinNumber { get; set; }
    public string File { get; set; } = string.Empty;
    public string? DisplayName { get; set; }

    public string Key => $"{ChampionId}:{SkinNumber}";
}
