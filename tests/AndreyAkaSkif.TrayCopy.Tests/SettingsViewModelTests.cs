namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class SettingsViewModelTests : IDisposable
{
    private const string DuplicateMessage = "Запись с таким именем уже есть";

    private static readonly AppSettings Loaded = new()
    {
        Entries = new EntryList(
            [
                new("github", "github-token"),
                new("gitea", "gitea-token"),
                new("gitlab", "gitlab-token"),
            ],
            "gitea"),
        Notification = NotificationKind.Popup,
        Protection = ProtectionMode.Dpapi,
        StartupDisplayTime = TimeSpan.FromSeconds(5),
        TrimWhitespace = true,
    };

    private readonly InMemorySettingsStore _store = new(new SettingsLoadResult(Loaded, null));
    private readonly FakeAutostart _autostart = new(isEnabled: false);
    private readonly FakeTimeProvider _time = new();
    private readonly SettingsService _settings;
    private readonly SettingsViewModel _viewModel;
    private bool _closeRequested;

    public SettingsViewModelTests()
    {
        _settings = new SettingsService(_store);
        _viewModel = new SettingsViewModel(_settings, _autostart, _time);
        _viewModel.CloseRequested += (_, _) => _closeRequested = true;
    }

    public void Dispose() => _viewModel.Dispose();

    [Fact]
    public void Constructor_ShouldFillFromCurrentSettingsAndAutostart()
    {
        // Arrange
        var autostart = new FakeAutostart(isEnabled: true);
        _settings.Update(settings => settings with
        {
            Notification = NotificationKind.System,
            Protection = ProtectionMode.None,
            StartupDisplayTime = TimeSpan.FromSeconds(12),
            TrimWhitespace = false,
        });

        // Act
        using var viewModel = new SettingsViewModel(_settings, autostart, _time);

        // Assert
        Assert.Equal(["github", "gitea", "gitlab"], viewModel.Entries.Select(entry => entry.Name));
        Assert.Equal("gitea-token", viewModel.Entries[1].Value);
        Assert.True(viewModel.IsSystemNotification);
        Assert.False(viewModel.IsPopupNotification);
        Assert.False(viewModel.IsProtected);
        Assert.Equal(12, viewModel.StartupDisplaySeconds);
        Assert.False(viewModel.TrimWhitespace);
        Assert.True(viewModel.IsAutostartEnabled);
        Assert.True(viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Constructor_ShouldSplitBackupPathFromSettingsService()
    {
        // Arrange
        var store = new InMemorySettingsStore(new SettingsLoadResult(
            new AppSettings(), @"C:\Data\TrayCopy\settings.json.20260924-220648.bak"));

        // Act
        using var viewModel = new SettingsViewModel(new SettingsService(store), _autostart, _time);

        // Assert
        Assert.Equal("settings.json.20260924-220648.bak", viewModel.BackupFileName);
        Assert.Equal(@"C:\Data\TrayCopy", viewModel.BackupFolder);
    }

    [Fact]
    public void Constructor_ShouldLeaveBackupEmpty_WhenSettingsWereRead()
    {
        // Assert
        Assert.Null(_viewModel.BackupFileName);
        Assert.Null(_viewModel.BackupFolder);
    }

    [Fact]
    public void Add_ShouldAppendSelectedInvalidEntryAndBlockSave()
    {
        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        var added = _viewModel.Entries[^1];
        Assert.Same(added, _viewModel.SelectedEntry);
        Assert.Equal(["Укажите имя"], NameErrors(added));
        Assert.Equal(["Укажите значение"], ValueErrors(added));
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Edit_ShouldUnblockSave_WhenAddedEntryIsFilled()
    {
        // Arrange
        _viewModel.AddCommand.Execute(null);
        var added = _viewModel.Entries[^1];

        // Act
        added.Name = "bitbucket";
        added.Value = "bitbucket-token";

        // Assert
        Assert.False(added.HasErrors);
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Edit_ShouldMarkBothEntries_WhenNamesRepeatIgnoringCase()
    {
        // Act
        _viewModel.Entries[2].Name = "GitHub";

        // Assert
        Assert.Equal([DuplicateMessage], NameErrors(_viewModel.Entries[0]));
        Assert.Equal([DuplicateMessage], NameErrors(_viewModel.Entries[2]));
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Edit_ShouldClearDuplicateOnBothEntries_WhenOneIsRenamed()
    {
        // Arrange
        _viewModel.Entries[2].Name = "github";

        // Act
        _viewModel.Entries[2].Name = "gitlab";

        // Assert
        Assert.False(_viewModel.Entries[0].HasErrors);
        Assert.False(_viewModel.Entries[2].HasErrors);
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Remove_ShouldClearDuplicate_WhenRepeatedEntryIsRemoved()
    {
        // Arrange
        _viewModel.Entries[2].Name = "github";
        _viewModel.SelectedEntry = _viewModel.Entries[2];

        // Act
        _viewModel.RemoveCommand.Execute(null);

        // Assert
        Assert.False(_viewModel.Entries[0].HasErrors);
        Assert.True(_viewModel.SaveCommand.CanExecute(null));
    }

    [Theory]
    [InlineData(true, true)]
    [InlineData(false, false)]
    public void Validation_ShouldTreatNamesAsDuplicates_OnlyWhenTrimmingIsOn(
        bool trimWhitespace, bool isDuplicate)
    {
        // Arrange
        _viewModel.TrimWhitespace = trimWhitespace;

        // Act
        _viewModel.Entries[2].Name = " github ";

        // Assert
        Assert.Equal(isDuplicate, _viewModel.Entries[2].HasErrors);
    }

    [Fact]
    public void Validation_ShouldRecheckEntries_WhenTrimmingIsSwitched()
    {
        // Arrange
        _viewModel.TrimWhitespace = false;
        _viewModel.Entries[2].Name = " github ";
        _viewModel.Entries[2].Value = "   ";
        Assert.False(_viewModel.Entries[2].HasErrors);

        // Act
        _viewModel.TrimWhitespace = true;

        // Assert
        Assert.Equal([DuplicateMessage], NameErrors(_viewModel.Entries[2]));
        Assert.Equal(["Укажите значение"], ValueErrors(_viewModel.Entries[2]));
    }

    [Fact]
    public void Commands_ShouldBeUnavailable_WhenNothingIsSelected()
    {
        // Assert
        Assert.Null(_viewModel.SelectedEntry);
        Assert.False(_viewModel.RemoveCommand.CanExecute(null));
        Assert.False(_viewModel.MoveUpCommand.CanExecute(null));
        Assert.False(_viewModel.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void MoveCommands_ShouldBeUnavailableAtEdges()
    {
        // Act & Assert
        _viewModel.SelectedEntry = _viewModel.Entries[0];
        Assert.False(_viewModel.MoveUpCommand.CanExecute(null));
        Assert.True(_viewModel.MoveDownCommand.CanExecute(null));

        _viewModel.SelectedEntry = _viewModel.Entries[^1];
        Assert.True(_viewModel.MoveUpCommand.CanExecute(null));
        Assert.False(_viewModel.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void MoveUp_ShouldMoveSelectedEntryAndKeepSelection()
    {
        // Arrange
        var gitlab = _viewModel.Entries[2];
        _viewModel.SelectedEntry = gitlab;

        // Act
        _viewModel.MoveUpCommand.Execute(null);

        // Assert
        Assert.Equal(["github", "gitlab", "gitea"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Same(gitlab, _viewModel.SelectedEntry);
    }

    [Fact]
    public void MoveDown_ShouldMoveSelectedEntryAndKeepSelection()
    {
        // Arrange
        var github = _viewModel.Entries[0];
        _viewModel.SelectedEntry = github;

        // Act
        _viewModel.MoveDownCommand.Execute(null);

        // Assert
        Assert.Equal(["gitea", "github", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Same(github, _viewModel.SelectedEntry);
    }

    [Fact]
    public void Remove_ShouldSelectNextEntry()
    {
        // Arrange
        _viewModel.SelectedEntry = _viewModel.Entries[1];

        // Act
        _viewModel.RemoveCommand.Execute(null);

        // Assert
        Assert.Equal(["github", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Equal("gitlab", _viewModel.SelectedEntry?.Name);
    }

    [Fact]
    public void Save_ShouldUpdateSettingsAndRequestClose()
    {
        // Arrange
        _viewModel.SelectedEntry = _viewModel.Entries[2];
        _viewModel.MoveUpCommand.Execute(null);
        _viewModel.Entries[0].Value = "  new-github-token  ";
        _viewModel.IsSystemNotification = true;
        _viewModel.IsProtected = false;
        _viewModel.StartupDisplaySeconds = 0;

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        var saved = Assert.Single(_store.Saved);
        Assert.Same(saved, _settings.Current);
        Assert.Equal(
            [
                new("github", "new-github-token"),
                new("gitlab", "gitlab-token"),
                new("gitea", "gitea-token"),
            ],
            saved.Entries.Items);
        Assert.Equal(NotificationKind.System, saved.Notification);
        Assert.Equal(ProtectionMode.None, saved.Protection);
        Assert.Equal(TimeSpan.Zero, saved.StartupDisplayTime);
        Assert.True(saved.TrimWhitespace);
        Assert.True(_closeRequested);
    }

    [Fact]
    public void Save_ShouldKeepWhitespace_WhenTrimmingIsOff()
    {
        // Arrange
        _viewModel.TrimWhitespace = false;
        _viewModel.Entries[0].Value = " github-token ";

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal(" github-token ", _settings.Current.Entries.Items[0].Value);
        Assert.False(_settings.Current.TrimWhitespace);
    }

    [Fact]
    public void Save_ShouldKeepCurrentEntry_WhenItIsRenamed()
    {
        // Arrange
        _viewModel.Entries[1].Name = "forgejo";

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("forgejo", _settings.Current.Entries.Current?.Name);
    }

    [Fact]
    public void Save_ShouldTakeCurrentEntryAtSaveTime()
    {
        // Arrange: пока окно открыто, текущую запись сменили из трея
        _settings.Update(settings => settings with { Entries = settings.Entries.SelectNext() });

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("gitlab", _settings.Current.Entries.Current?.Name);
    }

    [Fact]
    public void Save_ShouldWriteAutostart_OnlyWhenChanged()
    {
        // Arrange
        _viewModel.IsAutostartEnabled = true;
        _viewModel.IsAutostartEnabled = false;

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Empty(_autostart.Calls);
    }

    [Fact]
    public void Save_ShouldWriteAutostart_WhenChanged()
    {
        // Arrange
        _viewModel.IsAutostartEnabled = true;

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal([true], _autostart.Calls);
    }

    [Fact]
    public void Save_ShouldShowErrorAndStayOpen_WhenSaveFails()
    {
        // Arrange
        _store.SaveException = new IOException("Файл занят");

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("Не удалось сохранить: Файл занят", _viewModel.ErrorMessage);
        Assert.False(_closeRequested);
        Assert.Same(Loaded, _settings.Current);
    }

    [Fact]
    public void Cancel_ShouldRequestCloseWithoutSaving()
    {
        // Arrange
        _viewModel.Entries[0].Name = "changed";

        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.True(_closeRequested);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public void Exit_ShouldRequestExitWithoutSaving()
    {
        // Arrange
        var exitRequested = false;
        _viewModel.ExitRequested += (_, _) => exitRequested = true;

        // Act
        _viewModel.ExitCommand.Execute(null);

        // Assert
        Assert.True(exitRequested);
        Assert.Empty(_store.Saved);
    }

    [Fact]
    public void Countdown_ShouldRequestClose_WhenTimeIsOver()
    {
        // Arrange
        TestSynchronization.RunWithoutContext(
            () => _viewModel.Countdown.Start(TimeSpan.FromSeconds(5)));

        // Act
        _time.Advance(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(_closeRequested);
        Assert.Empty(_store.Saved);
    }

    private static string[] NameErrors(EntryViewModel entry) =>
        [.. entry.GetErrors(nameof(EntryViewModel.Name)).Cast<string>()];

    private static string[] ValueErrors(EntryViewModel entry) =>
        [.. entry.GetErrors(nameof(EntryViewModel.Value)).Cast<string>()];
}
