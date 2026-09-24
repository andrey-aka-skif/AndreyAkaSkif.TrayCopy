using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AndreyAkaSkif.TrayCopy.Settings;
using AndreyAkaSkif.TrayCopy.Settings.Persistence;
using AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;
using AndreyAkaSkif.TrayCopy.Tray;
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
            desktop.Exit += (_, _) => services.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }

    // Точка сборки зависимостей приложения. Сервисы создаются при первом обращении, а при
    // выходе контейнер освобождает созданные им объекты
    private static ServiceProvider ConfigureServices(
        IClassicDesktopStyleApplicationLifetime desktop)
    {
        var services = new ServiceCollection();

        services.AddSingleton(desktop);

        services.AddOptions<JsonSettingsStoreOptions>();
        services.AddSingleton<ISettingsStore, JsonSettingsStore>();
        services.AddSingleton<SettingsService>();

        services.AddSingleton<TrayController>();

        return services.BuildServiceProvider(new ServiceProviderOptions
        {
            ValidateOnBuild = true,
            ValidateScopes = true,
        });
    }
}
