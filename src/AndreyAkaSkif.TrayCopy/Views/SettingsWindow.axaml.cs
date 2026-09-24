using AndreyAkaSkif.TrayCopy.ViewModels;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;

namespace AndreyAkaSkif.TrayCopy.Views;

/// <summary>
/// Представляет окно настроек — основное окно приложения
/// </summary>
internal sealed partial class SettingsWindow : Window
{
    /// <summary>
    /// Создаёт окно без модели представления; нужен дизайнеру разметки
    /// </summary>
    public SettingsWindow()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Создаёт окно для модели представления <paramref name="viewModel"/>
    /// </summary>
    public SettingsWindow(SettingsViewModel viewModel)
        : this()
    {
        DataContext = viewModel;
        viewModel.CloseRequested += (_, _) => Close();

        // Действие пользователя в окне оставляет его открытым. Туннельная маршрутизация и
        // обработанные события — чтобы клик по полю или кнопке тоже считался; наведение
        // отсчёт не отменяет
        void CancelCountdown(object? sender, RoutedEventArgs e) => viewModel.Countdown.Cancel();
        AddHandler(PointerPressedEvent, CancelCountdown, RoutingStrategies.Tunnel, true);
        AddHandler(KeyDownEvent, CancelCountdown, RoutingStrategies.Tunnel, true);

        // Клик в поле строки не выделяет строку: нажатие забирает поле ввода. Выделение
        // нужно удалению и перемещению, поэтому строка выделяется по фокусу
        EntryList.AddHandler(GotFocusEvent, (_, e) =>
        {
            if (e.Source is Control { DataContext: EntryViewModel entry })
            {
                viewModel.SelectedEntry = entry;
            }
        });
    }
}
