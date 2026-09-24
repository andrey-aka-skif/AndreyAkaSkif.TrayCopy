using Avalonia;
using System;
using AndreyAkaSkif.TrayCopy.Activation;

namespace AndreyAkaSkif.TrayCopy;

sealed class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args)
    {
        // Повторный запуск при работающей копии просит её показать окно настроек и
        // завершается
        using var instance = SingleInstance.TryAcquire();
        if (instance is null)
        {
            SingleInstance.RequestActivation();
            return;
        }

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .WithInterFont()
            .LogToTrace();
}
