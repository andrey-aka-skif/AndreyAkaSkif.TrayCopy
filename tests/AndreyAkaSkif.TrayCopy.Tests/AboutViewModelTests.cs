using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Tests;

public class AboutViewModelTests
{
    private readonly FakeReleaseSource _source = new();
    private readonly AboutViewModel _viewModel;

    public AboutViewModelTests()
    {
        var checker = new UpdateChecker(
            _source, Options.Create(new UpdateCheckerOptions { CurrentVersion = "0.2.0" }));
        _viewModel = new AboutViewModel(checker);
    }

    // Окно открывается с надписью о проверке: команда запускается уже после показа
    [Fact]
    public void Status_ShouldBeCheckingBeforeFirstCheck()
    {
        // Assert
        Assert.Equal(UpdateStatus.Checking, _viewModel.Status);
        Assert.True(_viewModel.IsChecking);
        Assert.False(_viewModel.CanCheckAgain);
        Assert.Null(_viewModel.Update);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldOfferRelease_WhenNewerVersionExists()
    {
        // Arrange
        var release = FakeReleaseSource.Release("0.3.0");
        _source.Latest = release;

        // Act
        await _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UpdateStatus.UpdateAvailable, _viewModel.Status);
        Assert.True(_viewModel.IsUpdateAvailable);
        Assert.False(_viewModel.CanCheckAgain);
        Assert.Same(release, _viewModel.Update);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldReportUpToDate_WhenNoNewerVersion()
    {
        // Arrange
        _source.Latest = FakeReleaseSource.Release("0.2.0");

        // Act
        await _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UpdateStatus.UpToDate, _viewModel.Status);
        Assert.True(_viewModel.IsUpToDate);
        Assert.True(_viewModel.CanCheckAgain);
        Assert.Null(_viewModel.Update);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldReportFailure_WhenSourceThrows()
    {
        // Arrange
        _source.Exception = new HttpRequestException("Нет сети");

        // Act
        await _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UpdateStatus.Failed, _viewModel.Status);
        Assert.True(_viewModel.IsFailed);
        Assert.True(_viewModel.CanCheckAgain);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldFindRelease_WhenRepeatedAfterFailure()
    {
        // Arrange
        _source.Exception = new HttpRequestException("Нет сети");
        await _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        _source.Exception = null;
        _source.Latest = FakeReleaseSource.Release("0.3.0");

        // Act
        await _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Assert
        Assert.Equal(UpdateStatus.UpdateAvailable, _viewModel.Status);
        Assert.Equal(2, _source.Calls);
    }

    [Fact]
    public async Task CheckForUpdates_ShouldShowChecking_WhileSourceResponds()
    {
        // Arrange
        _source.Exception = new HttpRequestException("Нет сети");
        await _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        _source.Pending = new TaskCompletionSource<ReleaseInfo?>();

        // Act
        var check = _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);
        var statusWhileChecking = _viewModel.Status;
        _source.Pending.SetResult(null);
        await check;

        // Assert
        Assert.Equal(UpdateStatus.Checking, statusWhileChecking);
        Assert.Equal(UpdateStatus.UpToDate, _viewModel.Status);
    }

    // Отмена — закрытие окна: команда завершается без исключения и без надписи о сбое
    [Fact]
    public async Task CheckForUpdates_ShouldNotFail_WhenCanceled()
    {
        // Arrange
        _source.Pending = new TaskCompletionSource<ReleaseInfo?>();
        var check = _viewModel.CheckForUpdatesCommand.ExecuteAsync(null);

        // Act
        _viewModel.CheckForUpdatesCommand.Cancel();
        await check;

        // Assert
        Assert.Equal(UpdateStatus.Checking, _viewModel.Status);
        Assert.False(_viewModel.IsFailed);
    }
}
