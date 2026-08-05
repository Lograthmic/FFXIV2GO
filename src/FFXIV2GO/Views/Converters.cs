using System.Globalization;
using System.Windows;
using System.Windows.Data;
using System.Windows.Media;
using FFXIV2GO.Services;

namespace FFXIV2GO.Views;

public sealed class StepGlyphConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is StepState state
            ? state switch
            {
                StepState.Pending => "○",
                StepState.Running => "●",
                StepState.Completed => "✓",
                StepState.Failed => "✕",
                StepState.Skipped => "—",
                _ => "○"
            }
            : "○";

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StepColorConverter : IValueConverter
{
    private static readonly SolidColorBrush Completed = new(Color.FromRgb(0x10, 0x9D, 0x48));
    private static readonly SolidColorBrush Failed = new(Color.FromRgb(0xC4, 0x2B, 0x1C));
    private static readonly SolidColorBrush Running = new(Color.FromRgb(0x00, 0x64, 0xB3));
    private static readonly SolidColorBrush Neutral = new(Color.FromRgb(0x8A, 0x8A, 0x8A));

    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is StepState state
            ? state switch
            {
                StepState.Completed => Completed,
                StepState.Failed => Failed,
                StepState.Running => Running,
                _ => Neutral
            }
            : Neutral;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class StepProgressVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is StepState state && state == StepState.Running
            ? Visibility.Visible
            : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}

public sealed class BoolToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is bool b && b ? Visibility.Visible : Visibility.Collapsed;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
