using System.Text.Json.Serialization;

namespace AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;

/// <summary>
/// Предоставляет сгенерированные при сборке метаданные сериализации файла настроек
/// </summary>
/// <remarks>
/// Строгое чтение: пропущенное поле, <see langword="null"/> в ненулевом поле, неизвестное
/// или числовое значение перечисления дают <see cref="System.Text.Json.JsonException"/>,
/// то есть файл считается нечитаемым
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase,
    WriteIndented = true,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true,
    Converters =
    [
        typeof(CamelCaseEnumConverter<NotificationKind>),
        typeof(CamelCaseEnumConverter<ProtectionMode>),
    ])]
[JsonSerializable(typeof(SettingsFile))]
internal sealed partial class SettingsJsonContext : JsonSerializerContext;
