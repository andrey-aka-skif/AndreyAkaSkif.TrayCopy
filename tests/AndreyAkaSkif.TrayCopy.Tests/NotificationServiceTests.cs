namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class NotificationServiceTests
{
    private static readonly Notification Copied = new("Скопировано", "github");

    private readonly SettingsService _settings =
        new(new InMemorySettingsStore(new SettingsLoadResult(new AppSettings(), null)));

    private readonly FakeNotifier _popup = new(NotificationKind.Popup);
    private readonly FakeNotifier _system = new(NotificationKind.System);
    private readonly NotificationService _service;

    public NotificationServiceTests()
    {
        _service = new NotificationService(_settings, [_popup, _system]);
    }

    [Fact]
    public void Show_ShouldUseNotifierOfCurrentKind()
    {
        // Act
        _service.Show(Copied);

        // Assert
        Assert.Equal([Copied], _popup.Shown);
        Assert.Empty(_system.Shown);
    }

    [Fact]
    public void Show_ShouldFollowKindChangedInSettings()
    {
        // Arrange
        _settings.Update(settings => settings with { Notification = NotificationKind.System });

        // Act
        _service.Show(Copied);

        // Assert
        Assert.Empty(_popup.Shown);
        Assert.Equal([Copied], _system.Shown);
    }
}
