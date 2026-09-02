using InvoiceDesk.Wpf.ViewModels;

namespace InvoiceDesk.Wpf.Services;

/// <summary>
/// Lets a view model ask for another page without knowing what a shell is. The
/// shell listens; everything else calls. Going back is a request rather than a
/// destination, so the invoice editor does not have to know where it was opened
/// from — and cannot end up in a reference cycle with the page that opened it.
/// </summary>
public class NavigationService
{
    public event EventHandler<PageViewModel>? PageRequested;

    public event EventHandler? BackRequested;

    public void GoTo(PageViewModel page) => PageRequested?.Invoke(this, page);

    public void GoBack() => BackRequested?.Invoke(this, EventArgs.Empty);
}
