namespace AndreyAkaSkif.TrayCopy.Tests;

public class EntryTests
{
    [Fact]
    public void ToString_ShouldContainNameButNotValue()
    {
        // Arrange
        var entry = new Entry("github", "ghp_secret-token");

        // Act
        var text = entry.ToString();

        // Assert
        Assert.Contains("github", text, StringComparison.Ordinal);
        Assert.DoesNotContain("ghp_secret-token", text, StringComparison.Ordinal);
    }

    [Theory]
    [InlineData("", "ghp_token", "Name", "Укажите имя")]
    [InlineData("github", "", "Value", "Укажите значение")]
    public void Constructor_ShouldThrowWithRuleMessage_WhenDataBreaksRule(
        string name, string value, string paramName, string message)
    {
        // Act & Assert
        var exception = Assert.Throws<ArgumentException>(() => new Entry(name, value));
        Assert.Equal(paramName, exception.ParamName);
        Assert.StartsWith(message, exception.Message, StringComparison.Ordinal);
    }

    [Fact]
    public void With_ShouldThrow_WhenNewValueBreaksRule()
    {
        // Arrange
        var entry = new Entry("github", "ghp_token");

        // Act & Assert
        Assert.Throws<ArgumentException>(() => entry with { Value = "ghp_\ntoken" });
    }
}
