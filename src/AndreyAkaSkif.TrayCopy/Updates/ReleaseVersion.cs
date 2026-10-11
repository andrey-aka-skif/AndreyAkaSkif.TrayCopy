using System.Diagnostics.CodeAnalysis;
using System.Globalization;

namespace AndreyAkaSkif.TrayCopy.Updates;

/// <summary>
/// Представляет версию выпуска по SemVer 2.0.0: X.Y.Z с необязательной меткой после «-»
/// (0.2.1-dev.5, 0.0.0-local); метаданные сборки после «+» не принимаются
/// </summary>
/// <remarks>
/// Порядок — по SemVer: сначала числа, а при равных числах версия с меткой младше версии без
/// метки. Метки сравниваются по точкам: числовые части — как числа и младше буквенных,
/// буквенные — посимвольно; при общем начале старше метка с большим числом частей
/// </remarks>
internal sealed record ReleaseVersion : IComparable<ReleaseVersion>
{
    private ReleaseVersion(int major, int minor, int patch, string? prerelease)
    {
        Major = major;
        Minor = minor;
        Patch = patch;
        Prerelease = prerelease;
    }

    /// <summary>
    /// Старший номер версии
    /// </summary>
    public int Major { get; }

    /// <summary>
    /// Младший номер версии
    /// </summary>
    public int Minor { get; }

    /// <summary>
    /// Номер исправления
    /// </summary>
    public int Patch { get; }

    /// <summary>
    /// Метка предварительной версии без «-»; <see langword="null"/> у выпуска
    /// </summary>
    public string? Prerelease { get; }

    /// <summary>
    /// Разбирает версию из строки <paramref name="value"/>
    /// </summary>
    /// <exception cref="FormatException">Строка не в формате X.Y.Z[-метка]</exception>
    public static ReleaseVersion Parse(string value) =>
        TryParse(value, out var version)
            ? version
            : throw new FormatException($"Версия «{value}» не в формате X.Y.Z[-метка]");

    /// <summary>
    /// Разбирает версию из строки <paramref name="value"/>; возвращает
    /// <see langword="false"/>, если строка не в формате X.Y.Z[-метка]
    /// </summary>
    public static bool TryParse(
        [NotNullWhen(true)] string? value, [NotNullWhen(true)] out ReleaseVersion? version)
    {
        version = null;
        if (value is null)
        {
            return false;
        }

        var dash = value.IndexOf('-', StringComparison.Ordinal);
        var core = (dash < 0 ? value : value[..dash]).Split('.');
        var prerelease = dash < 0 ? null : value[(dash + 1)..];

        if (core.Length != 3
            || !TryParseNumber(core[0], out var major)
            || !TryParseNumber(core[1], out var minor)
            || !TryParseNumber(core[2], out var patch)
            || (prerelease is not null && !IsValidPrerelease(prerelease)))
        {
            return false;
        }

        version = new ReleaseVersion(major, minor, patch, prerelease);
        return true;
    }

    /// <summary>
    /// Сравнивает версию с <paramref name="other"/> по старшинству; любая версия старше
    /// <see langword="null"/>
    /// </summary>
    public int CompareTo(ReleaseVersion? other)
    {
        if (other is null)
        {
            return 1;
        }

        var result = Major.CompareTo(other.Major);
        if (result == 0)
        {
            result = Minor.CompareTo(other.Minor);
        }

        if (result == 0)
        {
            result = Patch.CompareTo(other.Patch);
        }

        return result != 0 ? result : ComparePrerelease(Prerelease, other.Prerelease);
    }

    /// <summary>
    /// Возвращает версию в виде X.Y.Z[-метка]
    /// </summary>
    public override string ToString() =>
        Prerelease is null ? $"{Major}.{Minor}.{Patch}" : $"{Major}.{Minor}.{Patch}-{Prerelease}";

    public static bool operator <(ReleaseVersion? left, ReleaseVersion? right) =>
        Compare(left, right) < 0;

    public static bool operator <=(ReleaseVersion? left, ReleaseVersion? right) =>
        Compare(left, right) <= 0;

    public static bool operator >(ReleaseVersion? left, ReleaseVersion? right) =>
        Compare(left, right) > 0;

    public static bool operator >=(ReleaseVersion? left, ReleaseVersion? right) =>
        Compare(left, right) >= 0;

    private static int Compare(ReleaseVersion? left, ReleaseVersion? right) =>
        left is null ? (right is null ? 0 : -1) : left.CompareTo(right);

    // Версия без метки старше версии с меткой
    private static int ComparePrerelease(string? left, string? right)
    {
        if (left is null || right is null)
        {
            return (left is null).CompareTo(right is null);
        }

        var leftParts = left.Split('.');
        var rightParts = right.Split('.');
        for (var i = 0; i < Math.Min(leftParts.Length, rightParts.Length); i++)
        {
            var result = ComparePrereleasePart(leftParts[i], rightParts[i]);
            if (result != 0)
            {
                return result;
            }
        }

        return leftParts.Length.CompareTo(rightParts.Length);
    }

    // Числа в метке — без ведущих нулей, поэтому большее число и записано длиннее. Сравнение
    // по длине, а при равной длине посимвольно, не переполняется на длинных числах
    private static int ComparePrereleasePart(string left, string right)
    {
        var isLeftNumeric = IsNumber(left);
        var isRightNumeric = IsNumber(right);
        if (isLeftNumeric != isRightNumeric)
        {
            return isLeftNumeric ? -1 : 1;
        }

        var result = isLeftNumeric ? left.Length.CompareTo(right.Length) : 0;
        return result != 0 ? result : string.CompareOrdinal(left, right);
    }

    private static bool TryParseNumber(string text, out int number)
    {
        number = 0;
        return IsNumber(text)
            && int.TryParse(text, NumberStyles.None, CultureInfo.InvariantCulture, out number);
    }

    // Метка — непустые части через точку из латинских букв, цифр и дефиса; числовые части —
    // без ведущих нулей
    private static bool IsValidPrerelease(string prerelease) =>
        prerelease.Split('.').All(part =>
            part.Length > 0
            && part.All(c => char.IsAsciiLetterOrDigit(c) || c == '-')
            && (!part.All(char.IsAsciiDigit) || IsNumber(part)));

    // Число по SemVer: только цифры и без ведущих нулей
    private static bool IsNumber(string text) =>
        text.Length > 0 && text.All(char.IsAsciiDigit) && (text.Length == 1 || text[0] != '0');
}
