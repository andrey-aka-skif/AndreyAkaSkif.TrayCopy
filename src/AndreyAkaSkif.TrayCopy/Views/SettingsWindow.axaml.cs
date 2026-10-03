using System.Collections.Specialized;
using AndreyAkaSkif.TrayCopy.Interop;
using AndreyAkaSkif.TrayCopy.ViewModels;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using Avalonia.Threading;
using Avalonia.VisualTree;

namespace AndreyAkaSkif.TrayCopy.Views;

/// <summary>
/// Представляет окно настроек — основное окно приложения
/// </summary>
internal sealed partial class SettingsWindow : Window
{
    // Классы кнопок строки, после которых фокус и курсор возвращаются к строке
    private const string MoveUpClass = "moveUp";
    private const string MoveDownClass = "moveDown";
    private const string RemoveClass = "remove";

    // Указатель последнего нажатия: курсор вслед за строкой переносится только у мыши
    private PointerType? _pointerType;

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

        AddHandler(
            PointerPressedEvent,
            (_, e) => _pointerType = e.Pointer.Type,
            RoutingStrategies.Tunnel,
            true);
        EntryList.AddHandler(Button.ClickEvent, OnEntryButtonClick);
        viewModel.Entries.CollectionChanged += OnEntriesChanged;
        EntryScroll.PropertyChanged += OnEntryScrollPropertyChanged;
    }

    // Папку открывает Проводник: запуск внешней программы — забота платформы, а не модели
    private void OnOpenBackupFolderClick(object? sender, RoutedEventArgs e)
    {
        if (DataContext is SettingsViewModel { BackupFolder: { } folder })
        {
            _ = Launcher.LaunchDirectoryInfoAsync(new DirectoryInfo(folder));
        }
    }

    private void OnAboutClick(object? sender, RoutedEventArgs e) =>
        _ = new AboutWindow().ShowDialog(this);

    // Перемещение пересоздаёт строку на новом месте, а на место удалённой встаёт следующая.
    // Фокус переходит к той же кнопке перемещённой строки, а после удаления — к кнопке
    // удаления строки, вставшей на её место. Курсор мыши едет вслед за перемещённой строкой,
    // чтобы повторный клик двигал ту же запись. Click поднимается до выполнения команды,
    // поэтому кнопка ещё на прежнем месте
    private void OnEntryButtonClick(object? sender, RoutedEventArgs e)
    {
        if (e.Source is not Button { DataContext: EntryViewModel entry } button
            || DataContext is not SettingsViewModel viewModel)
        {
            return;
        }

        var action = button.Classes.FirstOrDefault(
            name => name is MoveUpClass or MoveDownClass or RemoveClass);
        if (action is null)
        {
            return;
        }

        // Фокус с рамкой — только если он и был с рамкой, то есть пришёл с клавиатуры
        NavigationMethod? focus = !button.IsFocused ? null
            : button.Classes.Contains(":focus-visible") ? NavigationMethod.Tab
            : NavigationMethod.Pointer;
        PixelPoint? origin = action != RemoveClass
            && button.IsPointerOver
            && _pointerType == PointerType.Mouse
                ? button.PointToScreen(default)
                : null;
        var index = viewModel.Entries.IndexOf(entry);

        Dispatcher.UIThread.Post(
            () =>
            {
                if (action == RemoveClass)
                {
                    FocusAfterRemove(viewModel, index, focus);
                }
                else
                {
                    FollowMovedEntry(entry, action, focus, origin);
                }
            },
            DispatcherPriority.Loaded);
    }

    private void FollowMovedEntry(
        EntryViewModel entry, string action, NavigationMethod? focus, PixelPoint? origin)
    {
        if (FindEntryButton(entry, action) is not { } button)
        {
            return;
        }

        button.BringIntoView();
        UpdateLayout();

        // У строки, дошедшей до края, кнопка в ту же сторону недоступна; фокус остаётся у
        // строки — на кнопке в другую сторону
        if (focus is { } method)
        {
            var target = button.IsEffectivelyEnabled
                ? button
                : FindEntryButton(entry, action == MoveUpClass ? MoveDownClass : MoveUpClass);
            target?.Focus(method);
        }

        // Курсор сдвигается на столько же, на сколько сдвинулась кнопка: точка нажатия на
        // кнопке сохраняется
        if (origin is { } from && NativeMethods.GetCursorPos(out var cursor))
        {
            var to = button.PointToScreen(default);
            NativeMethods.SetCursorPos(cursor.X + to.X - from.X, cursor.Y + to.Y - from.Y);
        }
    }

    private void FocusAfterRemove(SettingsViewModel viewModel, int index, NavigationMethod? focus)
    {
        if (focus is not { } method)
        {
            return;
        }

        var entries = viewModel.Entries;
        Control? target = entries.Count == 0
            ? AddButton
            : FindEntryButton(entries[Math.Min(index, entries.Count - 1)], RemoveClass);
        target?.Focus(method);
    }

    // Добавленная или возвращённая строка прокручивается в видимую область, фокус — в её
    // поле имени
    private void OnEntriesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (e.Action != NotifyCollectionChangedAction.Add
            || e.NewItems?[0] is not EntryViewModel entry)
        {
            return;
        }

        Dispatcher.UIThread.Post(
            () =>
            {
                if (EntryList.ContainerFromItem(entry) is not { } container)
                {
                    return;
                }

                container.BringIntoView();
                container.GetVisualDescendants().OfType<TextBox>().FirstOrDefault()?.Focus();
            },
            DispatcherPriority.Loaded);
    }

    // Полоса прокрутки сужает строки списка на свою ширину — на столько же сдвигается нижняя
    // строка, чтобы её кнопки стояли в колонке кнопок строк. Пока список помещается целиком,
    // полоса недоступна (стиль по признаку scrollable)
    private void OnEntryScrollPropertyChanged(object? sender, AvaloniaPropertyChangedEventArgs e)
    {
        if (e.Property == ScrollViewer.ViewportProperty || e.Property == BoundsProperty)
        {
            var scrollBarWidth = Math.Max(0, EntryScroll.Bounds.Width - EntryScroll.Viewport.Width);
            EntryFooter.Margin = new Thickness(0, 0, scrollBarWidth, 0);
        }

        if (e.Property == ScrollViewer.ViewportProperty
            || e.Property == ScrollViewer.ExtentProperty)
        {
            EntryScroll.Classes.Set(
                "scrollable", EntryScroll.Extent.Height > EntryScroll.Viewport.Height);
        }
    }

    private Button? FindEntryButton(EntryViewModel entry, string action) =>
        EntryList.ContainerFromItem(entry)?.GetVisualDescendants()
            .OfType<Button>()
            .FirstOrDefault(button => button.Classes.Contains(action));
}
