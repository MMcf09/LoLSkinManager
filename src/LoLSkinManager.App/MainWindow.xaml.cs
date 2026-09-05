using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;

namespace LoLSkinManager.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<SkinPackage> _packages = new();
    private readonly ObservableCollection<ChampionCatalogItem> _champions = new();
    private readonly ObservableCollection<SkinCatalogItem> _skins = new();
    private readonly SkinLibraryService _library = new();
    private readonly RiotCatalogService _catalog = new();
    private readonly SkinPackageRepositoryService _packageRepository = new();
    private string _catalogVersion = string.Empty;

    public MainWindow()
    {
        InitializeComponent();
        PackagesList.ItemsSource = _packages;
        ChampionsList.ItemsSource = _champions;
        SkinsItemsControl.ItemsSource = _skins;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var package in await _library.LoadAsync())
                _packages.Add(package);

            RefreshState();
            await LoadCatalogAsync();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao iniciar o aplicativo:\n{ex.Message}", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async Task LoadCatalogAsync()
    {
        CatalogStatusText.Text = "Carregando catálogo oficial...";
        var result = await _catalog.LoadChampionsAsync();
        _catalogVersion = result.Version;

        _champions.Clear();
        foreach (var champion in result.Champions)
            _champions.Add(champion);

        CatalogStatusText.Text = $"Data Dragon {_catalogVersion} • {_champions.Count} campeões";

        if (_champions.Count > 0)
            ChampionsList.SelectedIndex = 0;
    }

    private async void ChampionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ChampionsList.SelectedItem is not ChampionCatalogItem champion || string.IsNullOrWhiteSpace(_catalogVersion))
            return;

        try
        {
            SelectedChampionText.Text = champion.Name;
            SkinCountText.Text = "Carregando skins...";
            _skins.Clear();

            var skins = await _catalog.LoadSkinsAsync(_catalogVersion, champion);
            foreach (var skin in skins)
                _skins.Add(skin);

            SkinCountText.Text = $"{_skins.Count} skins oficiais • clique em uma skin para procurar o .fantome mapeado";
        }
        catch (Exception ex)
        {
            SkinCountText.Text = "Falha ao carregar as skins.";
            MessageBox.Show($"Não foi possível carregar as skins de {champion.Name}:\n{ex.Message}", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Warning);
        }
    }

    private async void SkinCard_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not Button { Tag: SkinCatalogItem skin })
            return;

        try
        {
            SkinCountText.Text = $"Procurando pacote para {skin.Name}...";
            var mapping = await _packageRepository.FindPackageAsync(skin);

            if (mapping is null)
            {
                SkinCountText.Text = $"{skin.Name}: nenhum .fantome mapeado no repositório.";
                MessageBox.Show(
                    $"Ainda não existe um pacote .fantome mapeado para {skin.Name}.\n\nAdicione a entrada correspondente em skins/index.json e o arquivo em skins/packages/.",
                    "Pacote não encontrado",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
                return;
            }

            var existing = _packages.FirstOrDefault(p =>
                p.Name.Equals(mapping.DisplayName ?? skin.Name, StringComparison.OrdinalIgnoreCase));

            if (existing is not null)
            {
                foreach (var item in _packages)
                    item.IsEnabled = ReferenceEquals(item, existing);

                await SaveAndRefreshAsync();
                PackagesList.SelectedItem = existing;
                SkinCountText.Text = $"{skin.Name}: pacote já importado e selecionado no perfil.";
                return;
            }

            SkinCountText.Text = $"Baixando pacote de {skin.Name}...";
            var tempPath = await _packageRepository.DownloadPackageAsync(mapping);

            try
            {
                var package = await _library.ImportAsync(tempPath);
                package.Name = string.IsNullOrWhiteSpace(mapping.DisplayName) ? skin.Name : mapping.DisplayName!;

                foreach (var item in _packages)
                    item.IsEnabled = false;

                package.IsEnabled = true;
                _packages.Add(package);
                await SaveAndRefreshAsync();
                PackagesList.SelectedItem = package;

                SkinCountText.Text = $"{skin.Name}: .fantome importado e selecionado no perfil.";
                InfoText.Text = $"Selecionado: {package.Name}. O pacote foi preparado localmente; nenhuma injeção no processo do jogo foi realizada.";
            }
            finally
            {
                try { if (File.Exists(tempPath)) File.Delete(tempPath); } catch { }
            }
        }
        catch (Exception ex)
        {
            SkinCountText.Text = $"Falha ao preparar {skin.Name}.";
            MessageBox.Show($"Não foi possível preparar o pacote:\n{ex.Message}", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void ChampionSearchBox_TextChanged(object sender, TextChangedEventArgs e)
    {
        var view = System.Windows.Data.CollectionViewSource.GetDefaultView(ChampionsList.ItemsSource);
        if (view == null)
            return;

        var term = ChampionSearchBox.Text.Trim();
        view.Filter = item =>
        {
            if (item is not ChampionCatalogItem champion)
                return false;

            if (string.IsNullOrWhiteSpace(term))
                return true;

            return champion.Name.Contains(term, StringComparison.CurrentCultureIgnoreCase)
                   || champion.Title.Contains(term, StringComparison.CurrentCultureIgnoreCase);
        };
    }

    private async void Import_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog
        {
            Title = "Importar pacote de custom skin",
            Filter = "Pacotes de skin (*.zip;*.fantome)|*.zip;*.fantome|Todos os arquivos (*.*)|*.*",
            Multiselect = false
        };

        if (dialog.ShowDialog() != true)
            return;

        try
        {
            var package = await _library.ImportAsync(dialog.FileName);
            _packages.Add(package);
            await SaveAndRefreshAsync();
            InfoText.Text = $"Importado: {package.Name}";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível importar o pacote:\n{ex.Message}", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private async void Toggle_Click(object sender, RoutedEventArgs e)
    {
        if (PackagesList.SelectedItem is not SkinPackage package)
        {
            MessageBox.Show("Selecione um pacote primeiro.", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        package.IsEnabled = !package.IsEnabled;
        await SaveAndRefreshAsync();
        InfoText.Text = $"{package.Name}: {(package.IsEnabled ? "selecionado no perfil" : "desativado")}.";
    }

    private async void Remove_Click(object sender, RoutedEventArgs e)
    {
        if (PackagesList.SelectedItem is not SkinPackage package)
        {
            MessageBox.Show("Selecione um pacote primeiro.", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Information);
            return;
        }

        var answer = MessageBox.Show($"Remover '{package.Name}' da biblioteca?", "LoL Skin Manager", MessageBoxButton.YesNo, MessageBoxImage.Question);
        if (answer != MessageBoxResult.Yes)
            return;

        try
        {
            _library.DeletePackageFile(package);
            _packages.Remove(package);
            await SaveAndRefreshAsync();
            InfoText.Text = "Pacote removido.";
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Não foi possível remover o pacote:\n{ex.Message}", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
    }

    private void OpenFolder_Click(object sender, RoutedEventArgs e)
    {
        Directory.CreateDirectory(_library.PackagesDirectory);
        Process.Start(new ProcessStartInfo
        {
            FileName = _library.PackagesDirectory,
            UseShellExecute = true
        });
    }

    private async Task SaveAndRefreshAsync()
    {
        await _library.SaveAsync(_packages);
        PackagesList.Items.Refresh();
        RefreshState();
    }

    private void RefreshState()
    {
        EmptyState.Visibility = _packages.Count == 0 ? Visibility.Visible : Visibility.Collapsed;
    }
}
