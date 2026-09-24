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
}
