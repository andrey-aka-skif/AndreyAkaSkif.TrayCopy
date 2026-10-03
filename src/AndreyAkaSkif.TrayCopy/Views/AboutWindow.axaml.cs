using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;

namespace AndreyAkaSkif.TrayCopy.Views;

/// <summary>
/// Представляет окно «О программе»: версия, авторские права, лицензии и адрес репозитория
/// </summary>
internal sealed partial class AboutWindow : Window
{
    /// <summary>
    /// Создаёт окно
    /// </summary>
    public AboutWindow()
    {
        InitializeComponent();
    }

    // Файлы лицензий лежат рядом с exe и открываются программой, связанной с .txt
    private void OnLicenseClick(object? sender, RoutedEventArgs e) =>
        _ = Launcher.LaunchFileInfoAsync(new FileInfo(AppInfo.LicensePath));

    private void OnThirdPartyNoticesClick(object? sender, RoutedEventArgs e) =>
        _ = Launcher.LaunchFileInfoAsync(new FileInfo(AppInfo.ThirdPartyNoticesPath));

    private void OnCloseClick(object? sender, RoutedEventArgs e) => Close();
}
