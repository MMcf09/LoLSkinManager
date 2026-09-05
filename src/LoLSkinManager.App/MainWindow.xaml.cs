using Microsoft.Win32;
using System.Collections.ObjectModel;
using System.Diagnostics;
using System.IO;
using System.Windows;

namespace LoLSkinManager.App;

public partial class MainWindow : Window
{
    private readonly ObservableCollection<SkinPackage> _packages = new();
    private readonly SkinLibraryService _library = new();

    public MainWindow()
    {
        InitializeComponent();
        PackagesList.ItemsSource = _packages;
        Loaded += MainWindow_Loaded;
    }

    private async void MainWindow_Loaded(object sender, RoutedEventArgs e)
    {
        try
        {
            foreach (var package in await _library.LoadAsync())
                _packages.Add(package);

            RefreshState();
        }
        catch (Exception ex)
        {
            MessageBox.Show($"Falha ao carregar a biblioteca:\n{ex.Message}", "LoL Skin Manager", MessageBoxButton.OK, MessageBoxImage.Error);
        }
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
        InfoText.Text = $"{package.Name}: {(package.IsEnabled ? "ativado no perfil" : "desativado")}.";
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
