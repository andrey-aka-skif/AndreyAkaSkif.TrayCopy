using AndreyAkaSkif.TrayCopy.ViewModels;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AndreyAkaSkif.TrayCopy.Views;

/// <summary>
/// Представляет окно «О программе»: версия, проверка обновлений, авторские права, лицензии
/// и адрес репозитория
/// </summary>
internal sealed partial class AboutWindow : Window
{
    /// <summary>
    /// Создаёт окно без модели представления; нужен дизайнеру разметки
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Создаёт окно для модели представления <paramref name="viewModel"/>: проверка
    /// обновлений начинается при открытии окна и отменяется при закрытии
    /// </summary>
    public AboutWindow(AboutViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        Opened += (_, _) => viewModel.CheckForUpdatesCommand.Execute(null);
        Closed += (_, _) => viewModel.CheckForUpdatesCommand.Cancel();
    }

    // Файлы лицензий лежат рядом с exe и открываются программой, связанной с .txt
    private void OnLicenseClick(object? sender, RoutedEventArgs e) =>
        _ = Launcher.LaunchFileInfoAsync(new FileInfo(AppInfo.LicensePath));

    private void OnThirdPartyNoticesClick(object? sender, RoutedEventArgs e) =>
        _ = Launcher.LaunchFileInfoAsync(new FileInfo(AppInfo.ThirdPartyNoticesPath));

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
