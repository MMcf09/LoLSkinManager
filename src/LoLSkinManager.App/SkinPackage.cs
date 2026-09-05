namespace LoLSkinManager.App;

public sealed class SkinPackage
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Name { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
    public DateTime ImportedAt { get; set; } = DateTime.Now;
    public bool IsEnabled { get; set; }

    public string Status => IsEnabled ? "Ativado no perfil" : "Desativado";
}
