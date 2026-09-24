using System.Text.Json;
using System.Text.Json.Serialization;

namespace AndreyAkaSkif.TrayCopy.Settings.Persistence.Json;

/// <summary>
/// Преобразует значения перечисления в строки camelCase и обратно; числа не принимает
/// </summary>
internal sealed class CamelCaseEnumConverter<TEnum>()
    : JsonStringEnumConverter<TEnum>(JsonNamingPolicy.CamelCase, allowIntegerValues: false)
    where TEnum : struct, Enum;
