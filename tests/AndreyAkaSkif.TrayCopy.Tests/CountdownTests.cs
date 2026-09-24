namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class CountdownTests : IDisposable
{
    private readonly FakeTimeProvider _time = new();
    private readonly Countdown _countdown;

    public CountdownTests() => _countdown = new Countdown(_time);

    public void Dispose() => _countdown.Dispose();

    [Fact]
    public void Start_ShouldShowFullTime()
    {
        // Act
        Start(TimeSpan.FromSeconds(5));

        // Assert
        Assert.True(_countdown.IsRunning);
        Assert.Equal(5, _countdown.RemainingSeconds);
        Assert.Equal(1, _countdown.RemainingFraction);
    }

    [Fact]
    public void Tick_ShouldDecreaseRemainingTime()
    {
        // Arrange
        Start(TimeSpan.FromSeconds(5));

        // Act
        _time.Advance(TimeSpan.FromSeconds(2));

        // Assert
        Assert.True(_countdown.IsRunning);
        Assert.Equal(3, _countdown.RemainingSeconds);
        Assert.Equal(0.6, _countdown.RemainingFraction, precision: 2);
    }

    [Fact]
    public void Tick_ShouldRoundRemainingSecondsUp()
    {
        // Arrange
        Start(TimeSpan.FromSeconds(5));

        // Act
        _time.Advance(TimeSpan.FromMilliseconds(4100));

        // Assert
        Assert.Equal(1, _countdown.RemainingSeconds);
    }

    [Fact]
    public void Tick_ShouldRaiseElapsedOnceAndStop_WhenTimeIsOver()
    {
        // Arrange
        var elapsed = 0;
        _countdown.Elapsed += (_, _) => elapsed++;
        Start(TimeSpan.FromSeconds(5));

        // Act
        _time.Advance(TimeSpan.FromSeconds(5));
        _time.Advance(TimeSpan.FromSeconds(5));

        // Assert
        Assert.Equal(1, elapsed);
        Assert.False(_countdown.IsRunning);
        Assert.Equal(0, _countdown.RemainingFraction);
    }

    [Fact]
    public void Cancel_ShouldStopWithoutElapsed()
    {
        // Arrange
        var elapsed = false;
        _countdown.Elapsed += (_, _) => elapsed = true;
        Start(TimeSpan.FromSeconds(5));

        // Act
        _countdown.Cancel();
        _time.Advance(TimeSpan.FromSeconds(10));

        // Assert
        Assert.False(_countdown.IsRunning);
        Assert.False(elapsed);
    }

    [Fact]
    public void Start_ShouldRestart_WhenCountdownIsRunning()
    {
        // Arrange
        var elapsed = false;
        _countdown.Elapsed += (_, _) => elapsed = true;
        Start(TimeSpan.FromSeconds(5));
        _time.Advance(TimeSpan.FromSeconds(3));

        // Act
        Start(TimeSpan.FromSeconds(5));
        _time.Advance(TimeSpan.FromSeconds(3));

        // Assert
        Assert.False(elapsed);
        Assert.Equal(2, _countdown.RemainingSeconds);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-1)]
    public void Start_ShouldThrow_WhenDurationIsNotPositive(int seconds)
    {
        // Act & Assert
        Assert.Throws<ArgumentOutOfRangeException>(
            () => Start(TimeSpan.FromSeconds(seconds)));
    }

    private void Start(TimeSpan duration) =>
        TestSynchronization.RunWithoutContext(() => _countdown.Start(duration));
}
