using System;
using System.Globalization;
using Avalonia;
using Avalonia.Data.Converters;
using Avalonia.Media;
using FH6Mod.ViewModels;
using FH6Mod.ViewModels.Pages;

namespace FH6Mod.Converters;

public sealed class UpdateFooterColorConverter : IValueConverter
{
    public static readonly UpdateFooterColorConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is UpdateFooterStatus s ? new SolidColorBrush(s switch
        {
            UpdateFooterStatus.UpToDate         => Color.FromRgb(0x3D, 0xDC, 0x97),  // green
            UpdateFooterStatus.UpdateAvailable  => Color.FromRgb(0x7F, 0xBF, 0xFF),  // blue
            UpdateFooterStatus.Failed           => Color.FromRgb(0xFF, 0xB3, 0x47),  // amber
            _                                   => Color.FromRgb(0x9A, 0x9A, 0xAB),  // grey (checking)
        }) : Brushes.Gray;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StatusLabelConverter : IValueConverter
{
    public static readonly StatusLabelConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FeatureStatus s ? s switch
        {
            FeatureStatus.Working    => "WORKING",
            FeatureStatus.Untested   => "UNTESTED",
            FeatureStatus.NotWorking => "BROKEN",
            FeatureStatus.Locked     => "LOCKED",
            _ => "?",
        } : "?";

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StatusBgConverter : IValueConverter
{
    public static readonly StatusBgConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FeatureStatus s ? new SolidColorBrush(s switch
        {
            FeatureStatus.Working    => Color.FromArgb(38, 0x3D, 0xDC, 0x97),  // green tint
            FeatureStatus.Untested   => Color.FromArgb(38, 0x9A, 0x9A, 0xAB),  // grey tint
            FeatureStatus.NotWorking => Color.FromArgb(38, 0xFF, 0x5C, 0x5C),  // red tint
            FeatureStatus.Locked     => Color.FromArgb(38, 0xFF, 0xB3, 0x47),  // amber tint
            _ => Colors.Transparent,
        }) : Brushes.Transparent;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}

public sealed class StatusFgConverter : IValueConverter
{
    public static readonly StatusFgConverter Instance = new();

    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
        => value is FeatureStatus s ? new SolidColorBrush(s switch
        {
            FeatureStatus.Working    => Color.FromRgb(0x3D, 0xDC, 0x97),
            FeatureStatus.Untested   => Color.FromRgb(0xC7, 0xC7, 0xD2),
            FeatureStatus.NotWorking => Color.FromRgb(0xFF, 0x5C, 0x5C),
            FeatureStatus.Locked     => Color.FromRgb(0xFF, 0xB3, 0x47),
            _ => Colors.White,
        }) : Brushes.White;

    public object? ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture)
        => throw new NotSupportedException();
}
