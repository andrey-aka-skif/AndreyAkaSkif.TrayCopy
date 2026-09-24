using Microsoft.Extensions.Options;
using Microsoft.Win32;

namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class RunKeyAutostartTests : IDisposable
{
    private const string TestsKeyPath = @"Software\AndreyAkaSkif.TrayCopy.Tests";
    private const string ValueName = "AndreyAkaSkif.TrayCopy";
    private const string ExecutablePath = @"C:\Program Files\TrayCopy\AndreyAkaSkif.TrayCopy.exe";

    // Вместо настоящих ключей автозапуска — временный ключ теста в HKEY_CURRENT_USER
    private readonly string _keyPath = $@"{TestsKeyPath}\{Guid.NewGuid():N}";
    private readonly RunKeyAutostartOptions _options;
    private readonly RunKeyAutostart _autostart;

    public RunKeyAutostartTests()
    {
        _options = new RunKeyAutostartOptions
        {
            RunKeyPath = $@"{_keyPath}\Run",
            ApprovalKeyPath = $@"{_keyPath}\StartupApproved",
            ValueName = ValueName,
            ExecutablePath = ExecutablePath,
        };
        _autostart = new RunKeyAutostart(Options.Create(_options));
    }

    public void Dispose()
    {
        Registry.CurrentUser.DeleteSubKeyTree(_keyPath, throwOnMissingSubKey: false);

        using var testsKey = Registry.CurrentUser.OpenSubKey(TestsKeyPath);
        if (testsKey is { SubKeyCount: 0, ValueCount: 0 })
        {
            Registry.CurrentUser.DeleteSubKey(TestsKeyPath, throwOnMissingSubKey: false);
        }
    }

    [Fact]
    public void Options_ShouldPointToUserRunKeyAndCurrentProcess_ByDefault()
    {
        // Act
        var options = new RunKeyAutostartOptions();

        // Assert
        Assert.Equal(@"Software\Microsoft\Windows\CurrentVersion\Run", options.RunKeyPath);
        Assert.Equal(
            @"Software\Microsoft\Windows\CurrentVersion\Explorer\StartupApproved\Run",
            options.ApprovalKeyPath);
        Assert.Equal("AndreyAkaSkif.TrayCopy", options.ValueName);
        Assert.Equal(Environment.ProcessPath, options.ExecutablePath);
    }

    [Fact]
    public void IsEnabled_ShouldBeFalse_WhenValueIsMissing()
    {
        // Assert
        Assert.False(_autostart.IsEnabled);
    }

    [Fact]
    public void SetEnabled_ShouldWriteQuotedPathAndRemoveApproval_WhenEnabling()
    {
        // Arrange
        WriteApproval(0x03);

        // Act
        _autostart.SetEnabled(true);

        // Assert
        Assert.True(_autostart.IsEnabled);
        Assert.Equal($"\"{ExecutablePath}\"", ReadValue(_options.RunKeyPath));
        Assert.Null(ReadValue(_options.ApprovalKeyPath));
    }

    [Fact]
    public void IsEnabled_ShouldBeFalse_WhenTaskManagerDisabledIt()
    {
        // Arrange
        _autostart.SetEnabled(true);

        // Act
        WriteApproval(0x03);

        // Assert
        Assert.False(_autostart.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ShouldBeTrue_WhenTaskManagerApprovedIt()
    {
        // Arrange
        _autostart.SetEnabled(true);

        // Act
        WriteApproval(0x02);

        // Assert
        Assert.True(_autostart.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ShouldIgnorePathCase()
    {
        // Arrange
        WriteRunValue($"\"{ExecutablePath.ToUpperInvariant()}\"");

        // Assert
        Assert.True(_autostart.IsEnabled);
    }

    [Fact]
    public void IsEnabled_ShouldBeFalse_WhenValuePointsToAnotherCopy()
    {
        // Arrange
        WriteRunValue(@"""D:\Debug\AndreyAkaSkif.TrayCopy.exe""");

        // Assert
        Assert.False(_autostart.IsEnabled);
    }

    [Fact]
    public void SetEnabled_ShouldRemoveValueAndApproval_WhenDisabling()
    {
        // Arrange
        _autostart.SetEnabled(true);
        WriteApproval(0x03);

        // Act
        _autostart.SetEnabled(false);

        // Assert
        Assert.False(_autostart.IsEnabled);
        Assert.Null(ReadValue(_options.RunKeyPath));
        Assert.Null(ReadValue(_options.ApprovalKeyPath));
    }

    [Fact]
    public void SetEnabled_ShouldKeepAnotherCopyValue_WhenDisabling()
    {
        // Arrange
        const string otherCommand = @"""D:\Debug\AndreyAkaSkif.TrayCopy.exe""";
        WriteRunValue(otherCommand);

        // Act
        _autostart.SetEnabled(false);

        // Assert
        Assert.Equal(otherCommand, ReadValue(_options.RunKeyPath));
    }

    [Fact]
    public void SetEnabled_ShouldDoNothing_WhenDisablingMissingValue()
    {
        // Act
        _autostart.SetEnabled(false);

        // Assert
        Assert.Null(ReadValue(_options.RunKeyPath));
    }

    private void WriteRunValue(string command)
    {
        using var key = Registry.CurrentUser.CreateSubKey(_options.RunKeyPath);
        key.SetValue(ValueName, command, RegistryValueKind.String);
    }

    // Отметка Диспетчера задач: первый байт — состояние, дальше — время отключения
    private void WriteApproval(byte state)
    {
        var approval = new byte[12];
        approval[0] = state;
        using var key = Registry.CurrentUser.CreateSubKey(_options.ApprovalKeyPath);
        key.SetValue(ValueName, approval, RegistryValueKind.Binary);
    }

    private static object? ReadValue(string keyPath)
    {
        using var key = Registry.CurrentUser.OpenSubKey(keyPath);
        return key?.GetValue(ValueName);
    }
}
