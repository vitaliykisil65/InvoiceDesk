using System.Windows;
using System.Windows.Controls;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.ViewModels;

namespace InvoiceDesk.Wpf.Views;

public partial class ShellWindow : Window
{
    public ShellWindow(ShellViewModel viewModel)
    {
        InitializeComponent();
        DataContext = viewModel;
    }

    private void OnMinimizeClick(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void OnMaximizeRestoreClick(object sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void OnCloseClick(object sender, RoutedEventArgs e) => Close();

    protected override void OnStateChanged(EventArgs e)
    {
        base.OnStateChanged(e);

        var maximized = WindowState == WindowState.Maximized;

        // A chrome-less window overflows the work area when maximized, so the
        // root border compensates for the invisible resize frame.
        RootBorder.Padding = maximized ? new Thickness(7) : new Thickness(0);
        RootBorder.BorderThickness = maximized ? new Thickness(0) : new Thickness(1);
        MaximizeButton.Content = maximized ? "" : "";
        MaximizeButton.ToolTip = LocalizedStrings.Get(maximized ? "Shell_Restore" : "Shell_Maximize");
    }
}
