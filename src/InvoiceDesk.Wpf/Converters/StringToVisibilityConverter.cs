using System.Globalization;
using System.Windows;
using System.Windows.Data;

namespace InvoiceDesk.Wpf.Converters;

/// <summary>Shows an element only while its bound text has something to say.</summary>
public class StringToVisibilityConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        string.IsNullOrWhiteSpace(value as string) ? Visibility.Collapsed : Visibility.Visible;

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        throw new NotSupportedException();
}
