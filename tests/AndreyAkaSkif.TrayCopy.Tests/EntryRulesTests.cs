namespace AndreyAkaSkif.TrayCopy.Tests;

public class EntryRulesTests
{
    [Theory]
    [InlineData("", "Укажите имя")]
    [InlineData("   ", "Укажите имя")]
    [InlineData("git\nhub", "Имя должно быть одной строкой")]
    [InlineData("git\thub", "Имя должно быть одной строкой")]
    public void CheckName_ShouldReturnMessage_WhenNameBreaksRule(string name, string expected)
    {
        // Act
        var error = EntryRules.CheckName(name);

        // Assert
        Assert.Equal(expected, error);
    }

    [Fact]
    public void CheckName_ShouldReturnMessage_WhenNameIsTooLong()
    {
        // Act
        var error = EntryRules.CheckName(new string('a', EntryRules.MaxNameLength + 1));

        // Assert
        Assert.Equal("Имя длиннее 64 символов", error);
    }

    [Theory]
    [InlineData("github")]
    [InlineData(" github ")]
    [InlineData("мой токен")]
    public void CheckName_ShouldReturnNull_WhenNameIsValid(string name)
    {
        // Act
        var error = EntryRules.CheckName(name);

        // Assert
        Assert.Null(error);
    }

    [Fact]
    public void CheckName_ShouldAcceptNameOfMaxLength()
    {
        // Act
        var error = EntryRules.CheckName(new string('a', EntryRules.MaxNameLength));

        // Assert
        Assert.Null(error);
    }

    [Theory]
    [InlineData("", "Укажите значение")]
    [InlineData("ghp_\r\ntoken", "Значение должно быть одной строкой")]
    public void CheckValue_ShouldReturnMessage_WhenValueBreaksRule(string value, string expected)
    {
        // Act
        var error = EntryRules.CheckValue(value);

        // Assert
        Assert.Equal(expected, error);
    }

    [Theory]
    [InlineData("ghp_token")]
    [InlineData(" ghp_token ")]
    [InlineData("   ")]
    public void CheckValue_ShouldReturnNull_WhenValueIsValid(string value)
    {
        // Act
        var error = EntryRules.CheckValue(value);

        // Assert
        Assert.Null(error);
    }

    [Theory]
    [InlineData("gitea")]
    [InlineData("GitHub")]
    public void CheckUniqueName_ShouldReturnMessage_WhenNameIsTakenIgnoringCase(string name)
    {
        // Act
        var error = EntryRules.CheckUniqueName(name, ["github", "gitea"]);

        // Assert
        Assert.Equal("Запись с таким именем уже есть", error);
    }

    [Fact]
    public void CheckUniqueName_ShouldReturnNull_WhenNameIsFree()
    {
        // Act
        var error = EntryRules.CheckUniqueName("gitlab", ["github", "gitea"]);

        // Assert
        Assert.Null(error);
    }
}
