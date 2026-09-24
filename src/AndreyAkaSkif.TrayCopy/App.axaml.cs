using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Threading;
using AndreyAkaSkif.TrayCopy.Activation;
using AndreyAkaSkif.TrayCopy.Autostart;
using AndreyAkaSkif.TrayCopy.Autostart.RunKey;
using AndreyAkaSkif.TrayCopy.Settings;
using AndreyAkaSkif.TrayCopy.Settings.Persistence;
using AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;
using AndreyAkaSkif.TrayCopy.Tray;
using AndreyAkaSkif.TrayCopy.ViewModels;
using AndreyAkaSkif.TrayCopy.Views;
using Microsoft.Extensions.DependencyInjection;

namespace AndreyAkaSkif.TrayCopy;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            // Главного окна нет: приложение живёт иконкой в трее и завершается только явно
            desktop.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var services = ConfigureServices(desktop);
            services.GetRequiredService<TrayController>().Show();
            services.GetRequiredService<ActivationListener>().Start();

            // Окно при запуске показывается уже в цикле интерфейса: отсчёт до его скрытия
            // привязывается к контексту синхронизации этого цикла
            var settingsWindow = services.GetRequiredService<SettingsWindowService>();
            Dispatcher.UIThread.Post(settingsWindow.ShowAtStartup);

            desktop.Exit += (_, _) => services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Точка сборки зависимостей приложения. Сервисы создаются при первом обращении, а при
    // выходе контейнер освобождает созданные им объекты. Окно настроек и его модель живут
    // в области (scope), которую открывает и закрывает SettingsWindowService
    private static ServiceProvider ConfigureServices(
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        var services = new ServiceCollection();

        services.AddSingleton<IControlledApplicationLifetime>(desktop);
        services.AddSingleton(TimeProvider.System);

        services.AddOptions<JsonSettingsStoreOptions>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<SettingsService>();

        services.AddOptions<RunKeyAutostartOptions>();
        services.AddSingleton<IAutostart, RunKeyAutostart>();

        services.AddScoped<SettingsViewModel>();
        services.AddScoped<SettingsWindow>();
        services.AddSingleton<SettingsWindowService>();

        services.AddSingleton<ActivationListener>();
        services.AddSingleton<TrayIconHost>();
        services.AddSingleton<TrayController>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
