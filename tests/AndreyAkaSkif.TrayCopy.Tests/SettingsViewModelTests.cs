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
    public void Constructor_ShouldMarkCurrentEntry()
    {
        // Assert
        Assert.Same(_viewModel.Entries[1], _viewModel.CurrentEntry);
        Assert.Equal([false, true, false], _viewModel.Entries.Select(entry => entry.IsCurrent));
    }

    [Fact]
    public void Add_ShouldAppendInvalidEntryAndBlockSave()
    {
        // Act
        _viewModel.AddCommand.Execute(null);

        // Assert
        var added = _viewModel.Entries[^1];
        Assert.Equal(["Укажите имя"], NameErrors(added));
        Assert.Equal(["Укажите значение"], ValueErrors(added));
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
        Assert.Same(_viewModel.Entries[1], _viewModel.CurrentEntry);
    }

    [Fact]
    public void Add_ShouldMarkEntry_WhenListIsEmpty()
    {
        // Arrange
        var store = new InMemorySettingsStore(new SettingsLoadResult(new AppSettings(), null));
        using var viewModel = new SettingsViewModel(new SettingsService(store), _autostart, _time);
        Assert.Null(viewModel.CurrentEntry);

        // Act
        viewModel.AddCommand.Execute(null);

        // Assert
        Assert.True(Assert.Single(viewModel.Entries).IsCurrent);
    }

    [Fact]
    public void IsCurrent_ShouldMoveMarkAndNotifyBothEntries()
    {
        // Arrange
        var gitea = _viewModel.Entries[1];
        var gitlab = _viewModel.Entries[2];
        var changed = new List<EntryViewModel>();
        gitea.PropertyChanged += (_, e) => RecordIsCurrent(gitea, e.PropertyName);
        gitlab.PropertyChanged += (_, e) => RecordIsCurrent(gitlab, e.PropertyName);

        // Act
        gitlab.IsCurrent = true;

        // Assert
        Assert.Same(gitlab, _viewModel.CurrentEntry);
        Assert.False(gitea.IsCurrent);
        Assert.Equal([gitea, gitlab], changed);

        void RecordIsCurrent(EntryViewModel entry, string? propertyName)
        {
            if (propertyName == nameof(EntryViewModel.IsCurrent))
            {
                changed.Add(entry);
            }
        }
    }

    [Fact]
    public void IsCurrent_ShouldKeepMark_WhenUnchecked()
    {
        // Act
        _viewModel.Entries[1].IsCurrent = false;

        // Assert
        Assert.True(_viewModel.Entries[1].IsCurrent);
    }

    [Fact]
    public void SettingsChanged_ShouldMoveMark_WhenCurrentIsChangedFromTray()
    {
        // Arrange: строку переименовали в окне, но узнаётся она по сохранённому имени
        _viewModel.Entries[2].Name = "forgejo";

        // Act
        _settings.Update(settings => settings with { Entries = settings.Entries.SelectNext() });

        // Assert
        Assert.Same(_viewModel.Entries[2], _viewModel.CurrentEntry);
    }

    [Fact]
    public void SettingsChanged_ShouldKeepMark_WhenCurrentEntryIsRemovedInWindow()
    {
        // Arrange
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[2]);
        var current = _viewModel.CurrentEntry;

        // Act
        _settings.Update(settings => settings with { Entries = settings.Entries.SelectNext() });

        // Assert
        Assert.Same(current, _viewModel.CurrentEntry);
    }

    [Fact]
    public void SettingsChanged_ShouldBeIgnored_AfterDispose()
    {
        // Arrange
        _viewModel.Dispose();

        // Act
        _settings.Update(settings => settings with { Entries = settings.Entries.SelectNext() });

        // Assert
        Assert.Same(_viewModel.Entries[1], _viewModel.CurrentEntry);
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

        // Act
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[2]);

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
    public void UndoRemove_ShouldBeUnavailable_WhenNothingIsRemoved()
    {
        // Assert
        Assert.False(_viewModel.UndoRemoveCommand.CanExecute(null));
        Assert.Null(_viewModel.RemovedEntryMessage);
    }

    [Fact]
    public void MoveCommands_ShouldBeUnavailableAtEdges()
    {
        // Act & Assert
        Assert.False(_viewModel.MoveUpCommand.CanExecute(_viewModel.Entries[0]));
        Assert.True(_viewModel.MoveDownCommand.CanExecute(_viewModel.Entries[0]));

        Assert.True(_viewModel.MoveUpCommand.CanExecute(_viewModel.Entries[^1]));
        Assert.False(_viewModel.MoveDownCommand.CanExecute(_viewModel.Entries[^1]));
    }

    [Fact]
    public void MoveCommands_ShouldBeUnavailable_WithoutEntry()
    {
        // Assert: пока привязка не задала параметр, команда проверяется с null
        Assert.False(_viewModel.MoveUpCommand.CanExecute(null));
        Assert.False(_viewModel.MoveDownCommand.CanExecute(null));
    }

    [Fact]
    public void MoveUp_ShouldMoveEntryAndKeepCurrent()
    {
        // Act
        _viewModel.MoveUpCommand.Execute(_viewModel.Entries[2]);

        // Assert
        Assert.Equal(["github", "gitlab", "gitea"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Equal("gitea", _viewModel.CurrentEntry?.Name);
    }

    [Fact]
    public void MoveDown_ShouldMoveEntryAndKeepCurrent()
    {
        // Act
        _viewModel.MoveDownCommand.Execute(_viewModel.Entries[0]);

        // Assert
        Assert.Equal(["gitea", "github", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Equal("gitea", _viewModel.CurrentEntry?.Name);
    }

    [Fact]
    public void Remove_ShouldKeepCurrent_WhenOtherEntryIsRemoved()
    {
        // Act
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[0]);

        // Assert
        Assert.Equal(["gitea", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Equal("gitea", _viewModel.CurrentEntry?.Name);
    }

    [Fact]
    public void Remove_ShouldMarkFirstEntry_WhenCurrentIsRemoved()
    {
        // Act
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[1]);

        // Assert
        Assert.Equal(["github", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Same(_viewModel.Entries[0], _viewModel.CurrentEntry);
        Assert.True(_viewModel.Entries[0].IsCurrent);
    }

    [Fact]
    public void Remove_ShouldLeaveNoCurrent_WhenLastEntryIsRemoved()
    {
        // Act
        while (_viewModel.Entries.Count > 0)
        {
            _viewModel.RemoveCommand.Execute(_viewModel.Entries[0]);
        }

        // Assert
        Assert.Null(_viewModel.CurrentEntry);
    }

    [Fact]
    public void Remove_ShouldOfferUndo()
    {
        // Act
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[1]);

        // Assert
        Assert.Equal("Удалена запись «gitea»", _viewModel.RemovedEntryMessage);
        Assert.True(_viewModel.UndoRemoveCommand.CanExecute(null));
    }

    [Fact]
    public void Remove_ShouldNotOfferUndo_WhenEntryIsBlank()
    {
        // Arrange
        _viewModel.AddCommand.Execute(null);

        // Act
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[^1]);

        // Assert
        Assert.Null(_viewModel.RemovedEntryMessage);
        Assert.False(_viewModel.UndoRemoveCommand.CanExecute(null));
    }

    [Fact]
    public void UndoRemove_ShouldRestoreEntryAtFormerPosition()
    {
        // Arrange
        var github = _viewModel.Entries[0];
        _viewModel.RemoveCommand.Execute(github);

        // Act
        _viewModel.UndoRemoveCommand.Execute(null);

        // Assert
        Assert.Equal(["github", "gitea", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Same(github, _viewModel.Entries[0]);
        Assert.Equal("gitea", _viewModel.CurrentEntry?.Name);
        Assert.Null(_viewModel.RemovedEntryMessage);
        Assert.False(_viewModel.UndoRemoveCommand.CanExecute(null));
    }

    [Fact]
    public void UndoRemove_ShouldRestoreMark_WhenCurrentWasRemoved()
    {
        // Arrange
        var gitea = _viewModel.Entries[1];
        _viewModel.RemoveCommand.Execute(gitea);

        // Act
        _viewModel.UndoRemoveCommand.Execute(null);

        // Assert
        Assert.Same(gitea, _viewModel.CurrentEntry);
        Assert.False(_viewModel.Entries[0].IsCurrent);
    }

    [Fact]
    public void UndoRemove_ShouldMarkEntry_WhenListIsEmpty()
    {
        // Arrange: текущей отмечена gitlab, github удаляется не текущей
        _viewModel.Entries[2].IsCurrent = true;
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[0]);
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[0]);
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[0]);

        // Act
        _viewModel.UndoRemoveCommand.Execute(null);

        // Assert
        Assert.Equal("gitlab", Assert.Single(_viewModel.Entries).Name);
        Assert.Equal("gitlab", _viewModel.CurrentEntry?.Name);
    }

    [Fact]
    public void UndoRemove_ShouldRestoreEntriesInReverseOrder()
    {
        // Arrange
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[0]);
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[1]);

        // Act & Assert
        _viewModel.UndoRemoveCommand.Execute(null);
        Assert.Equal(["gitea", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Equal("Удалена запись «github»", _viewModel.RemovedEntryMessage);

        _viewModel.UndoRemoveCommand.Execute(null);
        Assert.Equal(["github", "gitea", "gitlab"], _viewModel.Entries.Select(entry => entry.Name));
        Assert.Null(_viewModel.RemovedEntryMessage);
    }

    [Fact]
    public void UndoRemove_ShouldRevalidateRestoredEntry()
    {
        // Arrange: значение из пробелов допустимо, пока обрезка выключена
        _viewModel.TrimWhitespace = false;
        _viewModel.Entries[2].Value = "   ";
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[2]);
        _viewModel.TrimWhitespace = true;

        // Act
        _viewModel.UndoRemoveCommand.Execute(null);

        // Assert
        Assert.Equal(["Укажите значение"], ValueErrors(_viewModel.Entries[2]));
        Assert.False(_viewModel.SaveCommand.CanExecute(null));
    }

    [Fact]
    public void Save_ShouldUpdateSettingsAndRequestClose()
    {
        // Arrange
        _viewModel.MoveUpCommand.Execute(_viewModel.Entries[2]);
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
    public void Save_ShouldKeepCurrentEntry_WhenItIsRemovedAndRestored()
    {
        // Arrange
        _viewModel.RemoveCommand.Execute(_viewModel.Entries[1]);
        _viewModel.UndoRemoveCommand.Execute(null);

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("gitea", _settings.Current.Entries.Current?.Name);
    }

    [Fact]
    public void Save_ShouldSaveMarkedEntryAsCurrent()
    {
        // Arrange
        _viewModel.Entries[2].IsCurrent = true;

        // Act
        _viewModel.SaveCommand.Execute(null);

        // Assert
        Assert.Equal("gitlab", _settings.Current.Entries.Current?.Name);
    }

    [Fact]
    public void Cancel_ShouldNotSaveMarkedEntry()
    {
        // Arrange
        _viewModel.Entries[2].IsCurrent = true;

        // Act
        _viewModel.CancelCommand.Execute(null);

        // Assert
        Assert.Equal("gitea", _settings.Current.Entries.Current?.Name);
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
