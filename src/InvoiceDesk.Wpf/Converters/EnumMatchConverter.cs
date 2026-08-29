using System.Globalization;
using System.Windows.Data;

namespace InvoiceDesk.Wpf.Converters;

/// <summary>
/// Binds a group of radio buttons to a single enum property: each button is
/// checked when the bound value equals its ConverterParameter, and checking a
/// button writes that value back.
/// </summary>
public class EnumMatchConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is not null && value.Equals(parameter);

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) =>
        value is true && parameter is not null ? parameter : Binding.DoNothing;
}
