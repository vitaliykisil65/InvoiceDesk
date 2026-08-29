using System.Windows.Data;
using System.Windows.Markup;

namespace InvoiceDesk.Wpf.Localization;

/// <summary>
/// XAML shorthand for a localized string: <c>{loc:Text Nav_Invoices}</c>.
/// It produces a binding to <see cref="LocalizedStrings"/> rather than a plain
/// value, which is what lets the text update when the language changes.
/// </summary>
[MarkupExtensionReturnType(typeof(string))]
public class TextExtension : MarkupExtension
{
    public TextExtension()
    {
    }

    public TextExtension(string key) => Key = key;

    [ConstructorArgument("key")]
    public string Key { get; set; } = string.Empty;

    public override object ProvideValue(IServiceProvider serviceProvider)
    {
        var binding = new Binding($"[{Key}]")
        {
            Source = LocalizedStrings.Instance,
            Mode = BindingMode.OneWay
        };

        return binding.ProvideValue(serviceProvider);
    }
}
