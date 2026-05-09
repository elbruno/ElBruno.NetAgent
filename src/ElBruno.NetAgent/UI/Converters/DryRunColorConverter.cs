using System;
using System.Windows.Data;
using System.Windows.Media;

namespace ElBruno.NetAgent.UI.Converters;

/// <summary>
/// Converts a boolean IsDryRun value to a foreground color.
/// True (dry-run) returns red; false returns green.
/// </summary>
public class DryRunColorConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        if (value is bool isDryRun)
        {
            return isDryRun ? Brushes.Red : Brushes.Green;
        }
        return Brushes.Black;
    }

    public object? ConvertBack(object? value, Type targetType, object? parameter, System.Globalization.CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
