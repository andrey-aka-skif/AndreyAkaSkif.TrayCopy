using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;

namespace AndreyAkaSkif.TrayCopy.Views;

/// <summary>
/// Переводит долю (от 0 до 1) в угол сектора в градусах
/// </summary>
internal sealed class FractionToAngleConverter : IValueConverter
{
    // Дуга ровно в 360° вырождается: её начало совпадает с концом
    private const double FullAngle = 359.9;

    /// <inheritdoc/>
    public object? Convert(
        object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is double fraction
            ? Math.Clamp(fraction, 0, 1) * FullAngle
            : AvaloniaProperty.UnsetValue;

    /// <inheritdoc/>
    public object? ConvertBack(
        object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
