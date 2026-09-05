using System.Text.Json;

namespace LoLSkinManager.App;

public sealed class SkinLibraryService
{
    private readonly JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };

    public string RootDirectory { get; } = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
        "LoLSkinManager");

    public string PackagesDirectory => Path.Combine(RootDirectory, "Packages");
    private string DatabasePath => Path.Combine(RootDirectory, "library.json");

    public SkinLibraryService()
    {
        Directory.CreateDirectory(PackagesDirectory);
    }

    public async Task<List<SkinPackage>> LoadAsync()
    {
        if (!File.Exists(DatabasePath))
            return new List<SkinPackage>();

        await using var stream = File.OpenRead(DatabasePath);
        return await JsonSerializer.DeserializeAsync<List<SkinPackage>>(stream, _jsonOptions)
               ?? new List<SkinPackage>();
    }

    public async Task SaveAsync(IEnumerable<SkinPackage> packages)
    {
        Directory.CreateDirectory(RootDirectory);
        await using var stream = File.Create(DatabasePath);
        await JsonSerializer.SerializeAsync(stream, packages, _jsonOptions);
    }

    public async Task<SkinPackage> ImportAsync(string sourcePath)
    {
        var extension = Path.GetExtension(sourcePath);
        var safeName = Path.GetFileNameWithoutExtension(sourcePath);
        var destination = Path.Combine(PackagesDirectory, $"{Guid.NewGuid():N}_{safeName}{extension}");

        await using (var input = File.OpenRead(sourcePath))
        await using (var output = File.Create(destination))
            await input.CopyToAsync(output);

        return new SkinPackage
        {
            Name = safeName,
            FilePath = destination,
            ImportedAt = DateTime.Now,
            IsEnabled = false
        };
    }

    public void DeletePackageFile(SkinPackage package)
    {
        if (File.Exists(package.FilePath))
            File.Delete(package.FilePath);
    }
}
