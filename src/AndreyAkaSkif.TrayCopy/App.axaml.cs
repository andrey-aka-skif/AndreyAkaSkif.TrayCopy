using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using AndreyAkaSkif.TrayCopy.Tray;

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

            var tray = new TrayController(() => desktop.Shutdown());
            tray.Show();
            desktop.Exit += (_, _) => tray.Dispose();
        }

        base.OnFrameworkInitializationCompleted();
    }
}
