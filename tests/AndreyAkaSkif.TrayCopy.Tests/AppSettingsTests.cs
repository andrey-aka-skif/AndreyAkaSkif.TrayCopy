namespace AndreyAkaSkif.TrayCopy.Tests;

public class AppSettingsTests
{
    [Fact]
    public void Constructor_ShouldShowWindowForFiveSecondsAndTrimWhitespace_ByDefault()
    {
        // Act
        var settings = new AppSettings();

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(5), settings.StartupDisplayTime);
        Assert.True(settings.TrimWhitespace);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(60)]
    public void StartupDisplayTime_ShouldAcceptBoundaries(int seconds)
    {
        // Act
        var settings = new AppSettings { StartupDisplayTime = TimeSpan.FromSeconds(seconds) };

        // Assert
        Assert.Equal(TimeSpan.FromSeconds(seconds), settings.StartupDisplayTime);
    }

    [Theory]
    [InlineData(-1)]
    [InlineData(61)]
    [InlineData(2.5)]
    public void StartupDisplayTime_ShouldThrow_WhenOutOfRangeOrFractional(double seconds)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => new AppSettings { StartupDisplayTime = TimeSpan.FromSeconds(seconds) });
    }
}
