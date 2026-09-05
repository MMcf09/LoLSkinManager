using System.Net.Http;
using System.Text.Json;

namespace LoLSkinManager.App;

public sealed class RiotCatalogService
{
    private readonly HttpClient _http = new();

    public async Task<(string Version, List<ChampionCatalogItem> Champions)> LoadChampionsAsync()
    {
        var versionsJson = await _http.GetStringAsync("https://ddragon.leagueoflegends.com/api/versions.json");
        var versions = JsonSerializer.Deserialize<List<string>>(versionsJson) ?? new List<string>();
        var version = versions.FirstOrDefault() ?? throw new InvalidOperationException("Não foi possível obter a versão atual do Data Dragon.");

        var championsJson = await _http.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{version}/data/pt_BR/champion.json");
        using var doc = JsonDocument.Parse(championsJson);
        var data = doc.RootElement.GetProperty("data");

        var champions = new List<ChampionCatalogItem>();
        foreach (var property in data.EnumerateObject())
        {
            var value = property.Value;
            var id = value.GetProperty("id").GetString() ?? property.Name;
            var name = value.GetProperty("name").GetString() ?? id;
            var title = value.TryGetProperty("title", out var titleEl) ? titleEl.GetString() ?? string.Empty : string.Empty;

            champions.Add(new ChampionCatalogItem
            {
                Id = id,
                Name = name,
                Title = title,
                IconUrl = $"https://ddragon.leagueoflegends.com/cdn/{version}/img/champion/{id}.png"
            });
        }

        return (version, champions.OrderBy(c => c.Name).ToList());
    }

    public async Task<List<SkinCatalogItem>> LoadSkinsAsync(string version, ChampionCatalogItem champion)
    {
        var json = await _http.GetStringAsync($"https://ddragon.leagueoflegends.com/cdn/{version}/data/pt_BR/champion/{champion.Id}.json");
        using var doc = JsonDocument.Parse(json);
        var champ = doc.RootElement.GetProperty("data").GetProperty(champion.Id);
        var skins = champ.GetProperty("skins");

        var result = new List<SkinCatalogItem>();
        foreach (var skin in skins.EnumerateArray())
        {
            var number = skin.GetProperty("num").GetInt32();
            var rawName = skin.GetProperty("name").GetString() ?? string.Empty;
            var skinName = rawName.Equals("default", StringComparison.OrdinalIgnoreCase)
                ? $"{champion.Name} Clássico"
                : rawName;

            result.Add(new SkinCatalogItem
            {
                ChampionId = champion.Id,
                ChampionName = champion.Name,
                Number = number,
                Name = skinName,
                HasChromas = skin.TryGetProperty("chromas", out var chromasEl) && chromasEl.GetBoolean(),
                SplashUrl = $"https://ddragon.leagueoflegends.com/cdn/img/champion/splash/{champion.Id}_{number}.jpg"
            });
        }

        return result;
    }
}
