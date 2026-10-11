namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class TrayActionsTests
{
    private static readonly AppSettings Loaded = new()
    {
        Entries = new EntryList(
            [
                new("github", "github-token"),
                new("gitea", "gitea-token"),
            ],
            "gitea"),
    };

    private readonly InMemorySettingsStore _store = new(new SettingsLoadResult(Loaded, null));
    private readonly FakeClipboard _clipboard = new();
    private readonly FakeNotifier _notifier = new(NotificationKind.Popup);
    private readonly SettingsService _settings;
    private readonly TrayActions _actions;
    private int _settingsRequests;
    private int _exitRequests;

    public TrayActionsTests()
    {
        _settings = new SettingsService(_store);
        _actions = new TrayActions(
            _settings, _clipboard, new NotificationService(_settings, [_notifier]));
        _actions.SettingsRequested += (_, _) => _settingsRequests++;
        _actions.ExitRequested += (_, _) => _exitRequests++;
    }

    [Fact]
    public void Click_ShouldCopyCurrentValueAndNotify()
    {
        // Act
        _actions.HandleClick(isShiftPressed: false, isControlPressed: false);

        // Assert
        AssertCopied();
    }

    [Fact]
    public void Click_ShouldNotifyFailure_WhenClipboardIsUnavailable()
    {
        // Arrange
        _clipboard.Exception = new IOException("Буфер обмена занят другой программой");

        // Act
        _actions.HandleClick(isShiftPressed: false, isControlPressed: false);

        // Assert
        Assert.Equal(
            [new Notification("Не удалось скопировать", "Буфер обмена занят другой программой")],
            _notifier.Shown);
    }

    [Fact]
    public void ClickWithControl_ShouldSelectAndSaveNextEntryAndNotify()
    {
        // Act
        _actions.HandleClick(isShiftPressed: false, isControlPressed: true);

        // Assert
        AssertSelectedNext();
    }

    [Fact]
    public void ClickWithControl_ShouldKeepCurrentAndNotifyFailure_WhenSaveFails()
    {
        // Arrange
        _store.SaveException = new IOException("Файл занят");

        // Act
        _actions.HandleClick(isShiftPressed: false, isControlPressed: true);

        // Assert
        AssertSelectionFailed();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void ClickWithShift_ShouldRequestSettingsOnly(bool isControlPressed)
    {
        // Act
        _actions.HandleClick(isShiftPressed: true, isControlPressed);

        // Assert
        AssertSettingsRequestedOnly();
    }

    [Theory]
    [InlineData(false)]
    [InlineData(true)]
    public void Click_ShouldRequestSettings_WhenListIsEmpty(bool isControlPressed)
    {
        // Arrange
        ClearEntries();

        // Act
        _actions.HandleClick(isShiftPressed: false, isControlPressed);

        // Assert
        AssertSettingsRequestedOnly();
    }

    [Fact]
    public void CreateMenu_ShouldListEntriesThenClickActionsThenApplication()
    {
        // Act
        var menu = _actions.CreateMenu();

        // Assert: текущая запись отмечена, у действий, доступных без меню, подписан жест
        Assert.Equal(
            [
                ["github", "gitea"],
                ["Копировать текущую — Клик", "Следующая запись — Ctrl+клик"],
                ["Настройки… — Shift+клик", "Выход"],
            ],
            Describe(menu));
        Assert.Equal([false, true], menu[0].Select(item => item.IsChecked));
        Assert.DoesNotContain(menu.Skip(1).SelectMany(group => group), item => item.IsChecked);
    }

    [Fact]
    public void CreateMenu_ShouldListApplicationOnly_WhenListIsEmpty()
    {
        // Arrange
        ClearEntries();

        // Act
        var menu = _actions.CreateMenu();

        // Assert
        Assert.Equal([["Настройки… — Shift+клик", "Выход"]], Describe(menu));
    }

    [Fact]
    public void EntryMenuItem_ShouldSelectAndSaveEntryWithoutCopyingOrNotifying()
    {
        // Act
        FindMenuItem("github").Click();

        // Assert
        Assert.Equal("github", _settings.Current.Entries.Current?.Name);
        Assert.Same(_settings.Current, Assert.Single(_store.Saved));
        Assert.Empty(_clipboard.Texts);
        Assert.Empty(_notifier.Shown);
    }

    [Fact]
    public void EntryMenuItem_ShouldDoNothing_WhenEntryIsCurrent()
    {
        // Act
        FindMenuItem("gitea").Click();

        // Assert
        Assert.Same(Loaded, _settings.Current);
        Assert.Empty(_store.Saved);
        Assert.Empty(_notifier.Shown);
    }

    [Fact]
    public void EntryMenuItem_ShouldKeepCurrentAndNotifyFailure_WhenSaveFails()
    {
        // Arrange
        _store.SaveException = new IOException("Файл занят");

        // Act
        FindMenuItem("github").Click();

        // Assert
        AssertSelectionFailed();
    }

    [Fact]
    public void CopyMenuItem_ShouldCopyCurrentValueAndNotify()
    {
        // Act
        FindMenuItem("Копировать текущую").Click();

        // Assert
        AssertCopied();
    }

    [Fact]
    public void NextMenuItem_ShouldSelectAndSaveNextEntryAndNotify()
    {
        // Act
        FindMenuItem("Следующая запись").Click();

        // Assert
        AssertSelectedNext();
    }

    [Fact]
    public void SettingsMenuItem_ShouldRequestSettingsOnly()
    {
        // Act
        FindMenuItem("Настройки…").Click();

        // Assert
        AssertSettingsRequestedOnly();
    }

    [Fact]
    public void ExitMenuItem_ShouldRequestExitOnly()
    {
        // Act
        FindMenuItem("Выход").Click();

        // Assert
        Assert.Equal(1, _exitRequests);
        Assert.Equal(0, _settingsRequests);
        Assert.Empty(_clipboard.Texts);
        Assert.Empty(_store.Saved);
        Assert.Empty(_notifier.Shown);
    }

    // Пункт меню как строка: текст и жест через тире
    private static IEnumerable<IEnumerable<string>> Describe(
        IReadOnlyList<IReadOnlyList<TrayMenuItem>> menu) =>
        menu.Select(group => group.Select(
            item => item.Gesture is null ? item.Text : $"{item.Text} — {item.Gesture}"));

    private TrayMenuItem FindMenuItem(string text) =>
        _actions.CreateMenu().SelectMany(group => group).Single(item => item.Text == text);

    private void ClearEntries()
    {
        _settings.Update(settings => settings with { Entries = EntryList.Empty });
        _store.Saved.Clear();
    }

    private void AssertCopied()
    {
        Assert.Equal(["gitea-token"], _clipboard.Texts);
        Assert.Equal([new Notification("Скопировано", "gitea")], _notifier.Shown);
        Assert.Empty(_store.Saved);
        Assert.Equal(0, _settingsRequests);
    }

    private void AssertSelectedNext()
    {
        // После последней записи — первая
        Assert.Equal("github", _settings.Current.Entries.Current?.Name);
        Assert.Same(_settings.Current, Assert.Single(_store.Saved));
        Assert.Equal([new Notification("Выбрано для копирования", "github")], _notifier.Shown);
        Assert.Empty(_clipboard.Texts);
    }

    private void AssertSelectionFailed()
    {
        Assert.Same(Loaded, _settings.Current);
        Assert.Equal([new Notification("Не удалось выбрать запись", "Файл занят")], _notifier.Shown);
    }

    private void AssertSettingsRequestedOnly()
    {
        Assert.Equal(1, _settingsRequests);
        Assert.Equal(0, _exitRequests);
        Assert.Empty(_clipboard.Texts);
        Assert.Empty(_store.Saved);
        Assert.Empty(_notifier.Shown);
    }
}
