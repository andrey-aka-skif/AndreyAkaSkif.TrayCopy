using System.Text.Json;
using System.Text.Json.Nodes;
using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Tests;

public sealed class JsonSettingsStoreTests : IDisposable
{
    // Корректный файл настроек; случаи нечитаемого файла портят в нём по одному полю
    private const string ValidFile = """
        {
          "version": 1,
          "notification": "popup",
          "protection": "none",
          "startupDisplaySeconds": 5,
          "trimWhitespace": true,
          "selected": "github",
          "entries": [{ "name": "github", "value": "ghp_token" }]
        }
        """;

    private readonly string _directory = Path.Combine(
        Path.GetTempPath(), "AndreyAkaSkif.TrayCopy.Tests", Guid.NewGuid().ToString("N"));

    private readonly string _path;
    private readonly JsonSettingsStore _store;

    public JsonSettingsStoreTests()
    {
        _path = Path.Combine(_directory, "settings.json");
        _store = new JsonSettingsStore(
            Options.Create(new JsonSettingsStoreOptions { FilePath = _path }));
    }

    public void Dispose()
    {
        if (Directory.Exists(_directory))
        {
            Directory.Delete(_directory, recursive: true);
        }
    }

    [Fact]
    public void FilePath_ShouldPointToUserAppData_ByDefault()
    {
        // Act
        var path = new JsonSettingsStoreOptions().FilePath;

        // Assert
        var expected = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "AndreyAkaSkif.TrayCopy",
            "settings.json");
        Assert.Equal(expected, path);
    }

    [Fact]
    public void Load_ShouldReturnDefaults_WhenFileIsMissing()
    {
        // Act
        var result = _store.Load();

        // Assert
        Assert.Null(result.BackupPath);
        AssertDefaults(result.Settings);
    }

    [Fact]
    public void Load_ShouldRestoreSavedSettings_WhenProtectionIsDpapi() =>
        AssertRoundtrip(ProtectionMode.Dpapi);

    [Fact]
    public void Load_ShouldRestoreSavedSettings_WhenProtectionIsNone() =>
        AssertRoundtrip(ProtectionMode.None);

    [Fact]
    public void Save_ShouldNotWritePlainValues_WhenProtectionIsDpapi()
    {
        // Act
        _store.Save(Sample(ProtectionMode.Dpapi));

        // Assert
        var json = File.ReadAllText(_path);
        Assert.DoesNotContain("ghp_first-token", json, StringComparison.Ordinal);
        Assert.DoesNotContain("gitea-second-token", json, StringComparison.Ordinal);
    }

    [Fact]
    public void Save_ShouldRewriteValues_WhenProtectionChanges()
    {
        // Arrange
        var settings = Sample(ProtectionMode.Dpapi);
        _store.Save(settings);

        // Act
        _store.Save(_store.Load().Settings with { Protection = ProtectionMode.None });
        var plainJson = File.ReadAllText(_path);
        var plain = _store.Load().Settings;

        _store.Save(plain with { Protection = ProtectionMode.Dpapi });
        var protectedJson = File.ReadAllText(_path);
        var restored = _store.Load().Settings;

        // Assert
        Assert.Contains("ghp_first-token", plainJson, StringComparison.Ordinal);
        Assert.Equal(settings.Entries.Items, plain.Entries.Items);

        Assert.DoesNotContain("ghp_first-token", protectedJson, StringComparison.Ordinal);
        Assert.Equal(settings.Entries.Items, restored.Entries.Items);
    }

    [Fact]
    public void Save_ShouldWriteDocumentedFormat()
    {
        // Act
        _store.Save(Sample(ProtectionMode.None));

        // Assert
        using var document = JsonDocument.Parse(File.ReadAllText(_path));
        var root = document.RootElement;
        Assert.Equal(1, root.GetProperty("version").GetInt32());
        Assert.Equal("system", root.GetProperty("notification").GetString());
        Assert.Equal("none", root.GetProperty("protection").GetString());
        Assert.Equal(10, root.GetProperty("startupDisplaySeconds").GetInt32());
        Assert.False(root.GetProperty("trimWhitespace").GetBoolean());
        Assert.Equal("gitea", root.GetProperty("selected").GetString());

        var first = root.GetProperty("entries")[0];
        Assert.Equal("github", first.GetProperty("name").GetString());
        Assert.Equal("ghp_first-token", first.GetProperty("value").GetString());
    }

    [Fact]
    public void Save_ShouldWriteDefaultModesAndNullSelected_WhenSettingsAreDefault()
    {
        // Act
        _store.Save(new AppSettings());

        // Assert
        using var document = JsonDocument.Parse(File.ReadAllText(_path));
        var root = document.RootElement;
        Assert.Equal("popup", root.GetProperty("notification").GetString());
        Assert.Equal("dpapi", root.GetProperty("protection").GetString());
        Assert.Equal(5, root.GetProperty("startupDisplaySeconds").GetInt32());
        Assert.True(root.GetProperty("trimWhitespace").GetBoolean());
        Assert.Equal(JsonValueKind.Null, root.GetProperty("selected").ValueKind);
        Assert.Equal(0, root.GetProperty("entries").GetArrayLength());
    }

    [Fact]
    public void Load_ShouldReadFile_WhenFileIsValid()
    {
        // Arrange
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, ValidFile);

        // Act
        var result = _store.Load();

        // Assert
        Assert.Null(result.BackupPath);
        var settings = result.Settings;
        Assert.Equal(NotificationKind.Popup, settings.Notification);
        Assert.Equal(ProtectionMode.None, settings.Protection);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.StartupDisplayTime);
        Assert.True(settings.TrimWhitespace);
        Assert.Equal([new Entry("github", "ghp_token")], settings.Entries.Items);
        Assert.Equal("github", settings.Entries.Current?.Name);
    }

    [Fact]
    public void Save_ShouldCreateDirectoryAndLeaveOnlySettingsFile()
    {
        // Arrange
        Assert.False(Directory.Exists(_directory));

        // Act
        _store.Save(Sample(ProtectionMode.Dpapi));

        // Assert
        Assert.Equal([_path], Directory.GetFiles(_directory));
    }

    public static TheoryData<string> UnreadableFiles =>
    [
        "{ not json",
        "null",
        ValidFileWith(root => root["version"] = 2),
        ValidFileWith(root => root.Remove("notification")),
        ValidFileWith(root => root["notification"] = "toast"),
        ValidFileWith(root => root["notification"] = 0),
        ValidFileWith(root => root.Remove("startupDisplaySeconds")),
        ValidFileWith(root => root["startupDisplaySeconds"] = -1),
        ValidFileWith(root => root["startupDisplaySeconds"] = 61),
        ValidFileWith(root => root.Remove("trimWhitespace")),
        ValidFileWith(root => root.Remove("selected")),
        ValidFileWith(root => root["entries"] = null),
        ValidFileWith(root => root["entries"]![0]!["name"] = null),
        ValidFileWith(root => root["entries"]![0]!.AsObject().Remove("value")),
        // данные нарушают правила записей
        ValidFileWith(root => root["entries"]![0]!["name"] = ""),
        ValidFileWith(root => root["entries"]![0]!["value"] = ""),
        ValidFileWith(root => root["entries"]!.AsArray().Add(
            new JsonObject { ["name"] = "GitHub", ["value"] = "second-token" })),
        // значение при dpapi — не base64
        ValidFileWith(root => root["protection"] = "dpapi"),
        // значение при dpapi — base64, но не шифр DPAPI этого пользователя
        ValidFileWith(root =>
        {
            root["protection"] = "dpapi";
            root["entries"]![0]!["value"] = "Z2FyYmFnZQ==";
        }),
    ];

    [Theory]
    [MemberData(nameof(UnreadableFiles))]
    public void Load_ShouldBackUpFileAndReturnDefaults_WhenFileIsUnreadable(string content)
    {
        // Arrange
        Directory.CreateDirectory(_directory);
        File.WriteAllText(_path, content);

        // Act
        var result = _store.Load();

        // Assert
        AssertDefaults(result.Settings);
        Assert.False(File.Exists(_path));

        Assert.NotNull(result.BackupPath);
        Assert.Equal(_directory, Path.GetDirectoryName(result.BackupPath));
        Assert.Matches(@"^settings\.json\.\d{8}-\d{6}\.bak$", Path.GetFileName(result.BackupPath));
        Assert.Equal(content, File.ReadAllText(result.BackupPath));
    }

    private void AssertRoundtrip(ProtectionMode protection)
    {
        // Arrange
        var settings = Sample(protection);

        // Act
        _store.Save(settings);
        var result = _store.Load();

        // Assert
        Assert.Null(result.BackupPath);
        var loaded = result.Settings;
        Assert.Equal(settings.Notification, loaded.Notification);
        Assert.Equal(settings.Protection, loaded.Protection);
        Assert.Equal(settings.StartupDisplayTime, loaded.StartupDisplayTime);
        Assert.Equal(settings.TrimWhitespace, loaded.TrimWhitespace);
        Assert.Equal(settings.Entries.Items, loaded.Entries.Items);
        Assert.Equal(settings.Entries.Current, loaded.Entries.Current);
    }

    private static string ValidFileWith(Action<JsonObject> spoil)
    {
        var root = JsonNode.Parse(ValidFile)!.AsObject();
        spoil(root);
        return root.ToJsonString();
    }

    private static void AssertDefaults(AppSettings settings)
    {
        Assert.Equal(NotificationKind.Popup, settings.Notification);
        Assert.Equal(ProtectionMode.Dpapi, settings.Protection);
        Assert.Equal(TimeSpan.FromSeconds(5), settings.StartupDisplayTime);
        Assert.True(settings.TrimWhitespace);
        Assert.Empty(settings.Entries.Items);
        Assert.Null(settings.Entries.Current);
    }

    private static AppSettings Sample(ProtectionMode protection) => new()
    {
        Notification = NotificationKind.System,
        Protection = protection,
        StartupDisplayTime = TimeSpan.FromSeconds(10),
        TrimWhitespace = false,
        Entries = new EntryList(
            [new("github", "ghp_first-token"), new("gitea", "gitea-second-token")],
            "gitea"),
    };
}
