using AndreyAkaSkif.TrayCopy.Settings;
using AndreyAkaSkif.TrayCopy.ViewModels;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Microsoft.Extensions.DependencyInjection;

namespace AndreyAkaSkif.TrayCopy.Views;

/// <summary>
/// Показывает окно настроек. Окно одно: пока оно открыто, повторный показ выводит его вперёд
/// </summary>
/// <remarks>
/// Каждое открытие создаёт окно и его модель представления в отдельной области (scope)
/// контейнера, а закрытие окна освобождает её: состояние окна не переживает закрытия, и
/// следующее открытие начинается с текущих настроек
/// </remarks>
/// <param name="scopeFactory">Создаёт область контейнера для окна</param>
/// <param name="settings">Текущие настройки: по ним решается, показывать ли окно при старте</param>
/// <param name="lifetime">Завершает приложение по кнопке «Выйти из приложения»</param>
internal sealed class SettingsWindowService(
    IServiceScopeFactory scopeFactory,
    SettingsService settings,
    IControlledApplicationLifetime lifetime)
{
    private SettingsWindow? _window;
    private SettingsViewModel? _viewModel;

    /// <summary>
    /// Показывает окно по запросу пользователя — без отсчёта до скрытия. Открытое окно
    /// выводит вперёд, отменяя отсчёт
    /// </summary>
    public void Show()
    {
        if (_window is null)
        {
            Open(activate: true);
            return;
        }

        _viewModel!.Countdown.Cancel();
        if (_window.WindowState == WindowState.Minimized)
        {
            _window.WindowState = WindowState.Normal;
        }

        _window.Activate();
    }

    /// <summary>
    /// Показывает окно при запуске приложения. Пустой список — окно остаётся открытым;
    /// иначе окно показывается на время из настроек и скрывается само, а при нулевом
    /// времени не показывается
    /// </summary>
    public void ShowAtStartup()
    {
        var current = settings.Current;
        if (current.Entries.Items.Count == 0)
        {
            Show();
            return;
        }

        if (current.StartupDisplayTime == TimeSpan.Zero || _window is not null)
        {
            return;
        }

        // Без активации: окно, показанное при входе в Windows, не перехватывает ввод
        Open(activate: false);
        _viewModel!.Countdown.Start(current.StartupDisplayTime);
    }

    private void Open(bool activate)
    {
        var scope = scopeFactory.CreateScope();
        var window = scope.ServiceProvider.GetRequiredService<SettingsWindow>();
        var viewModel = scope.ServiceProvider.GetRequiredService<SettingsViewModel>();

        window.ShowActivated = activate;
        viewModel.ExitRequested += (_, _) => lifetime.Shutdown();
        window.Closed += (_, _) =>
        {
            _window = null;
            _viewModel = null;
            scope.Dispose();
        };

        _window = window;
        _viewModel = viewModel;
        window.Show();
    }
}
