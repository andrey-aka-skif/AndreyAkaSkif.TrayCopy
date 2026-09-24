using System.Globalization;
using System.Security.Cryptography;
using System.Text.Json;
using AndreyAkaSkif.TrayCopy.Entries;
using Microsoft.Extensions.Options;

namespace AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;

/// <summary>
/// Хранит настройки приложения в JSON-файле
/// </summary>
/// <param name="options">Параметры хранилища: путь к файлу</param>
internal sealed class JsonSettingsStore(IOptions<JsonSettingsStoreOptions> options)
    : ISettingsStore
{
    private readonly string _path = Path.GetFullPath(options.Value.FilePath);

    /// <summary>
    /// Читает настройки из файла. Если файла нет, возвращает настройки по умолчанию. Если
    /// файл нечитаем — повреждён, неполон, содержит недопустимые данные, зашифрован другим
    /// пользователем или записан в неизвестной версии формата, — переносит его в резервную
    /// копию рядом и возвращает настройки по умолчанию вместе с путём копии
    /// </summary>
    /// <exception cref="IOException">Файл недоступен, например занят другим процессом</exception>
    public SettingsLoadResult Load()
    {
        if (!File.Exists(_path))
        {
            return new SettingsLoadResult(new AppSettings(), BackupPath: null);
        }

        AppSettings? settings;
        try
        {
            settings = Read();
        }
        // ArgumentException — недопустимые данные: модель отклоняет их при создании
        catch (Exception e) when (e is JsonException or CryptographicException
            or FormatException or ArgumentException)
        {
            settings = null;
        }

        if (settings is not null)
        {
            return new SettingsLoadResult(settings, BackupPath: null);
        }

        // Метка времени в имени: повторная порча не затирает прежнюю копию
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss", CultureInfo.InvariantCulture);
        var backupPath = $"{_path}.{timestamp}.bak";
        File.Move(_path, backupPath);
        return new SettingsLoadResult(new AppSettings(), backupPath);
    }

    /// <summary>
    /// Сохраняет настройки в файл, шифруя значения записей согласно
    /// <see cref="AppSettings.Protection"/>. Файл заменяется целиком: сбой посреди записи
    /// не портит прежнее содержимое
    /// </summary>
    public void Save(AppSettings settings)
    {
        var file = new SettingsFile(
            SettingsFile.CurrentVersion,
            settings.Notification,
            settings.Protection,
            (int)settings.StartupDisplayTime.TotalSeconds,
            settings.TrimWhitespace,
            settings.Entries.Current?.Name,
            ConvertValues(
                settings.Entries.Items,
                value => ValueProtector.Protect(value, settings.Protection)));

        Directory.CreateDirectory(Path.GetDirectoryName(_path)!);

        var tempPath = _path + ".tmp";
        using (var stream = File.Create(tempPath))
        {
            JsonSerializer.Serialize(stream, file, SettingsJsonContext.Default.SettingsFile);
            stream.Flush(flushToDisk: true);
        }

        File.Move(tempPath, _path, overwrite: true);
    }

    // Возвращает null, если файл содержит JSON null или записан в другой версии формата
    private AppSettings? Read()
    {
        SettingsFile? file;
        using (var stream = File.OpenRead(_path))
        {
            file = JsonSerializer.Deserialize(stream, SettingsJsonContext.Default.SettingsFile);
        }

        if (file is null || file.Version != SettingsFile.CurrentVersion)
        {
            return null;
        }

        var entries = ConvertValues(
            file.Entries,
            value => ValueProtector.Unprotect(value, file.Protection));

        return new AppSettings
        {
            Entries = new EntryList(entries, file.Selected),
            Notification = file.Notification,
            Protection = file.Protection,
            StartupDisplayTime = TimeSpan.FromSeconds(file.StartupDisplaySeconds),
            TrimWhitespace = file.TrimWhitespace,
        };
    }

    private static Entry[] ConvertValues(
        IEnumerable<Entry> entries, Func<string, string> convert) =>
        [.. entries.Select(entry => entry with { Value = convert(entry.Value) })];
}
