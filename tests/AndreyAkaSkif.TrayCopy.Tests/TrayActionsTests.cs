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

    public TrayActionsTests()
    {
        _settings = new SettingsService(_store);
        _actions = new TrayActions(
            _settings, _clipboard, new NotificationService(_settings, [_notifier]));
        _actions.SettingsRequested += (_, _) => _settingsRequests++;
    }

    [Fact]
    public void LeftClick_ShouldCopyCurrentValueAndNotify()
    {
        // Act
        _actions.HandleClick(TrayButton.Left, isShiftPressed: false);

        // Assert
        Assert.Equal(["gitea-token"], _clipboard.Texts);
        Assert.Equal([new Notification("Скопировано", "gitea")], _notifier.Shown);
        Assert.Empty(_store.Saved);
        Assert.Equal(0, _settingsRequests);
    }

    [Fact]
    public void LeftClick_ShouldNotifyFailure_WhenClipboardIsUnavailable()
    {
        // Arrange
        _clipboard.Exception = new IOException("Буфер обмена занят другой программой");

        // Act
        _actions.HandleClick(TrayButton.Left, isShiftPressed: false);

        // Assert
        Assert.Equal(
            [new Notification("Не удалось скопировать", "Буфер обмена занят другой программой")],
            _notifier.Shown);
    }

    [Fact]
    public void RightClick_ShouldSelectAndSaveNextEntryAndNotify()
    {
        // Act
        _actions.HandleClick(TrayButton.Right, isShiftPressed: false);

        // Assert: после последней записи — первая
        Assert.Equal("github", _settings.Current.Entries.Current?.Name);
        Assert.Same(_settings.Current, Assert.Single(_store.Saved));
        Assert.Equal([new Notification("Выбрано для копирования", "github")], _notifier.Shown);
        Assert.Empty(_clipboard.Texts);
    }

    [Fact]
    public void RightClick_ShouldKeepCurrentAndNotifyFailure_WhenSaveFails()
    {
        // Arrange
        _store.SaveException = new IOException("Файл занят");

        // Act
        _actions.HandleClick(TrayButton.Right, isShiftPressed: false);

        // Assert
        Assert.Same(Loaded, _settings.Current);
        Assert.Equal([new Notification("Не удалось выбрать запись", "Файл занят")], _notifier.Shown);
    }

    [Theory]
    [InlineData(nameof(TrayButton.Left))]
    [InlineData(nameof(TrayButton.Right))]
    public void ClickWithShift_ShouldRequestSettingsOnly(string button)
    {
        // Act
        _actions.HandleClick(Enum.Parse<TrayButton>(button), isShiftPressed: true);

        // Assert
        Assert.Equal(1, _settingsRequests);
        Assert.Empty(_clipboard.Texts);
        Assert.Empty(_store.Saved);
        Assert.Empty(_notifier.Shown);
    }

    [Theory]
    [InlineData(nameof(TrayButton.Left))]
    [InlineData(nameof(TrayButton.Right))]
    public void Click_ShouldRequestSettings_WhenListIsEmpty(string button)
    {
        // Arrange
        _settings.Update(settings => settings with { Entries = EntryList.Empty });
        _store.Saved.Clear();

        // Act
        _actions.HandleClick(Enum.Parse<TrayButton>(button), isShiftPressed: false);

        // Assert
        Assert.Equal(1, _settingsRequests);
        Assert.Empty(_clipboard.Texts);
        Assert.Empty(_store.Saved);
        Assert.Empty(_notifier.Shown);
    }
}
