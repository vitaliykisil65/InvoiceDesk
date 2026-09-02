using System.Windows;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Asks the user to confirm something they cannot undo. View models call this
/// instead of opening a dialog themselves, so the question stays testable and
/// the wording stays localized.
/// </summary>
public class ConfirmationService
{
    public bool Ask(string question) =>
        MessageBox.Show(
            question,
            LocalizedStrings.Get("Common_ConfirmTitle"),
            MessageBoxButton.OKCancel,
            MessageBoxImage.Question) == MessageBoxResult.OK;
}
