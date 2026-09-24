namespace AndreyAkaSkif.TrayCopy.Tests;

public class EntryListTests
{
    private static readonly Entry[] Items =
    [
        new("github", "github-token"),
        new("gitea", "gitea-token"),
        new("gitlab", "gitlab-token"),
    ];

    [Fact]
    public void Empty_ShouldHaveNoItemsAndNoCurrent()
    {
        // Act
        var list = EntryList.Empty;

        // Assert
        Assert.Empty(list.Items);
        Assert.Null(list.Current);
    }

    [Fact]
    public void Constructor_ShouldLeaveNoCurrent_WhenItemsAreEmpty()
    {
        // Act
        var list = new EntryList([], "github");

        // Assert
        Assert.Null(list.Current);
    }

    [Theory]
    [InlineData("github")]
    [InlineData("gitea")]
    [InlineData("gitlab")]
    public void Constructor_ShouldSelectNamedEntry_WhenNameIsFound(string name)
    {
        // Act
        var list = new EntryList(Items, name);

        // Assert
        Assert.Equal(name, list.Current?.Name);
    }

    [Theory]
    [InlineData(null)]
    [InlineData("unknown")]
    // имена сравниваются с учётом регистра
    [InlineData("GitHub")]
    public void Constructor_ShouldSelectFirstEntry_WhenNameIsNotFound(string? name)
    {
        // Act
        var list = new EntryList(Items, name);

        // Assert
        Assert.Equal("github", list.Current?.Name);
    }

    [Fact]
    public void Constructor_ShouldCopyItems()
    {
        // Arrange
        var source = new List<Entry>(Items);

        // Act
        var list = new EntryList(source, "github");
        source.Clear();

        // Assert
        Assert.Equal(Items, list.Items);
    }

    [Theory]
    [InlineData("github", "gitea")]
    [InlineData("gitea", "gitlab")]
    public void SelectNext_ShouldSelectFollowingEntry_WhenCurrentIsNotLast(
        string current, string expected)
    {
        // Arrange
        var list = new EntryList(Items, current);

        // Act
        var next = list.SelectNext();

        // Assert
        Assert.Equal(expected, next.Current?.Name);
    }

    [Fact]
    public void SelectNext_ShouldSelectFirstEntry_WhenCurrentIsLast()
    {
        // Arrange
        var list = new EntryList(Items, "gitlab");

        // Act
        var next = list.SelectNext();

        // Assert
        Assert.Equal("github", next.Current?.Name);
    }

    [Fact]
    public void SelectNext_ShouldKeepItemsAndLeaveOriginalUnchanged()
    {
        // Arrange
        var list = new EntryList(Items, "github");

        // Act
        var next = list.SelectNext();

        // Assert
        Assert.Equal(Items, next.Items);
        Assert.Equal("github", list.Current?.Name);
    }

    [Fact]
    public void SelectNext_ShouldKeepSameEntry_WhenListHasSingleEntry()
    {
        // Arrange
        var list = new EntryList([new("github", "github-token")], "github");

        // Act
        var next = list.SelectNext();

        // Assert
        Assert.Equal("github", next.Current?.Name);
    }

    [Fact]
    public void SelectNext_ShouldLeaveNoCurrent_WhenListIsEmpty()
    {
        // Act
        var next = EntryList.Empty.SelectNext();

        // Assert
        Assert.Null(next.Current);
    }
}
