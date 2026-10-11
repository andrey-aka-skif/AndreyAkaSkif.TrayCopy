using System.Text.Json.Serialization;

namespace AndreyAkaSkif.TrayCopy.Updates.GitHub;

/// <summary>
/// Предоставляет сгенерированные при сборке метаданные чтения ответов GitHub REST API
/// </summary>
/// <remarks>
/// Нужные поля обязательны: пропущенное поле или <see langword="null"/> в нём дают
/// <see cref="System.Text.Json.JsonException"/>. Остальные поля ответа пропускаются
/// </remarks>
[JsonSourceGenerationOptions(
    PropertyNamingPolicy = JsonKnownNamingPolicy.SnakeCaseLower,
    RespectNullableAnnotations = true,
    RespectRequiredConstructorParameters = true)]
[JsonSerializable(typeof(GitHubRelease))]
internal sealed partial class GitHubJsonContext : JsonSerializerContext;
