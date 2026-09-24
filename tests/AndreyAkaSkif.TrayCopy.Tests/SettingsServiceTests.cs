namespace AndreyAkaSkif.TrayCopy.Tests;

public class SettingsServiceTests
{
    private static readonly AppSettings Loaded = new()
    {
        Entries = new EntryList(
            [new("github", "github-token"), new("gitea", "gitea-token")],
            "github"),
    };

    [Fact]
    public void Constructor_ShouldTakeSettingsAndBackupPathFromStore()
    {
        // Arrange
        var store = new InMemorySettingsStore(new SettingsLoadResult(Loaded, "settings.json.bak"));

        // Act
        var service = new SettingsService(store);

        // Assert
        Assert.Same(Loaded, service.Current);
        Assert.Equal("settings.json.bak", service.BackupPath);
    }

    [Fact]
    public void Update_ShouldApplyChangeToCurrentSettings()
    {
        // Arrange
        var service = new SettingsService(new InMemorySettingsStore(new(Loaded, null)));
        AppSettings? received = null;

        // Act
        service.Update(settings =>
        {
            received = settings;
            return settings;
        });

        // Assert
        Assert.Same(Loaded, received);
    }

    [Fact]
    public void Update_ShouldSaveAndReplaceCurrent()
    {
        // Arrange
        var store = new InMemorySettingsStore(new(Loaded, null));
        var service = new SettingsService(store);

        // Act
        service.Update(settings => settings with { Entries = settings.Entries.SelectNext() });

        // Assert
        var saved = Assert.Single(store.Saved);
        Assert.Same(saved, service.Current);
        Assert.Equal("gitea", service.Current.Entries.Current?.Name);
    }

    [Fact]
    public void Update_ShouldRaiseChangedAfterCurrentIsReplaced()
    {
        // Arrange
        var service = new SettingsService(new InMemorySettingsStore(new(Loaded, null)));
        var updated = Loaded with { Notification = NotificationKind.System };
        AppSettings? currentOnChanged = null;
        service.Changed += (_, _) => currentOnChanged = service.Current;

        // Act
        service.Update(_ => updated);

        // Assert
        Assert.Same(updated, currentOnChanged);
    }

    [Fact]
    public void Update_ShouldKeepCurrentAndNotRaiseChanged_WhenSaveFails()
    {
        // Arrange
        var store = new InMemorySettingsStore(new(Loaded, null))
        {
            SaveException = new IOException("Файл занят"),
        };
        var service = new SettingsService(store);
        var changed = false;
        service.Changed += (_, _) => changed = true;

        // Act & Assert
        Assert.Throws<IOException>(
            () => service.Update(settings => settings with { Protection = ProtectionMode.None }));
        Assert.Same(Loaded, service.Current);
        Assert.False(changed);
    }
}
