namespace LoLSkinManager.App;

public sealed class SkinCatalogItem
{
    public string ChampionId { get; init; } = string.Empty;
    public string ChampionName { get; init; } = string.Empty;
    public int Number { get; init; }
    public string Name { get; init; } = string.Empty;
    public bool HasChromas { get; init; }
    public string SplashUrl { get; init; } = string.Empty;

    public string ChromaLabel => HasChromas ? "Possui chromas" : "Skin oficial";
}
