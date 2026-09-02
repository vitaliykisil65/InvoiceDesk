using System.Collections.ObjectModel;
using System.Globalization;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// Every invoice in one table, with the filters a small business actually
/// reaches for: what is still owed, who owes it, and where a given number went.
/// Filtering happens on the loaded list, so a keystroke never waits on the
/// database.
/// </summary>
public partial class InvoicesViewModel : PageViewModel
{
    private readonly IInvoiceStore _store;

    private readonly ConfirmationService _confirmation;

    private readonly InvoiceEditorViewModel _editor;

    private readonly NavigationService _navigation;

    private List<Invoice> _loaded = [];

    /// <summary>Totals of what the filters currently leave on screen.</summary>
    private decimal _shownIssuedTotal;

    private decimal _shownOutstandingTotal;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private StatusFilterOption _selectedStatus;

    [ObservableProperty]
    private ClientFilterOption _selectedClient;

    [ObservableProperty]
    private InvoiceRowViewModel? _selectedInvoice;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public InvoicesViewModel(
        IInvoiceStore store,
        ConfirmationService confirmation,
        InvoiceEditorViewModel editor,
        NavigationService navigation)
    {
        _store = store;
        _confirmation = confirmation;
        _editor = editor;
        _navigation = navigation;

        _selectedStatus = StatusFilterOption.All();
        _selectedClient = ClientFilterOption.All();

        StatusOptions = [_selectedStatus, .. Enum.GetValues<InvoiceStatus>().Select(StatusFilterOption.For)];
        ClientOptions = [_selectedClient];
    }

    public override string TitleKey => "Nav_Invoices";

    public override string Icon => "";

    public ObservableCollection<InvoiceRowViewModel> Invoices { get; } = [];

    public ObservableCollection<StatusFilterOption> StatusOptions { get; }

    public ObservableCollection<ClientFilterOption> ClientOptions { get; }

    public bool IsEmpty => Invoices.Count == 0;

    public string ListSummary => LocalizedStrings.Format(
        "Invoices_Summary",
        Invoices.Count,
        FormatMoney(_shownIssuedTotal),
        FormatMoney(_shownOutstandingTotal));

    /// <summary>Reloads the list whenever the shell navigates here.</summary>
    public override Task OnActivatedAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(cancellationToken);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var selectedId = SelectedInvoice?.Id;

        _loaded = [.. await _store.GetAsync(cancellationToken)];

        BuildClientOptions();
        ApplyFilter();

        SelectedInvoice = Invoices.FirstOrDefault(invoice => invoice.Id == selectedId);
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();

        // Status names, dates and money all follow the interface language, and
        // the filter lists carry their own labels.
        BuildStatusOptions();
        BuildClientOptions();
        ApplyFilter();
    }

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedStatusChanged(StatusFilterOption value) => ApplyFilter();

    partial void OnSelectedClientChanged(ClientFilterOption value) => ApplyFilter();

    partial void OnSelectedInvoiceChanged(InvoiceRowViewModel? value)
    {
        StatusMessage = string.Empty;
        MarkAsSentCommand.NotifyCanExecuteChanged();
        DeleteDraftCommand.NotifyCanExecuteChanged();
        OpenInvoiceCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Starts a draft in the editor; the list reloads on the way back.</summary>
    [RelayCommand]
    private void NewInvoice()
    {
        _editor.Open(null);
        _navigation.GoTo(_editor);
    }

    /// <summary>
    /// Opens the selected invoice. Anything that was sent opens read-only, so
    /// this is as much a way to look at an invoice as a way to change one.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanOpenInvoice))]
    private void OpenInvoice()
    {
        if (SelectedInvoice is null)
        {
            return;
        }

        _editor.Open(SelectedInvoice.Id);
        _navigation.GoTo(_editor);
    }

    private bool CanOpenInvoice() => SelectedInvoice is not null;

    /// <summary>Hands a draft over to the client: from here on it is owed.</summary>
    [RelayCommand(CanExecute = nameof(CanActOnDraft))]
    private async Task MarkAsSentAsync()
    {
        var invoice = LoadedSelection();

        if (invoice is null)
        {
            return;
        }

        try
        {
            IsBusy = true;

            invoice.Status = InvoiceStatus.Sent;
            await _store.SaveAsync(invoice);

            StatusMessage = LocalizedStrings.Format("Invoices_SentStatus", invoice.Number);
            await LoadAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    /// <summary>
    /// Only a draft can be deleted. An invoice that was ever sent stays on the
    /// books, because somebody outside the company has seen it.
    /// </summary>
    [RelayCommand(CanExecute = nameof(CanActOnDraft))]
    private async Task DeleteDraftAsync()
    {
        var invoice = LoadedSelection();

        if (invoice is null || !_confirmation.Ask(LocalizedStrings.Format("Invoices_DeleteConfirm", invoice.Number)))
        {
            return;
        }

        try
        {
            IsBusy = true;

            await _store.DeleteDraftAsync(invoice.Id);
            StatusMessage = LocalizedStrings.Format("Invoices_DeletedStatus", invoice.Number);

            SelectedInvoice = null;
            await LoadAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
        }
    }

    private bool CanActOnDraft() => !IsBusy && SelectedInvoice?.IsDraft == true;

    private Invoice? LoadedSelection() =>
        _loaded.FirstOrDefault(invoice => invoice.Id == SelectedInvoice?.Id);

    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        var matches = _loaded
            .Where(invoice => SelectedStatus.Matches(invoice))
            .Where(invoice => SelectedClient.Matches(invoice))
            .Where(invoice => query.Length == 0 || Matches(invoice, query))
            .ToList();

        Invoices.Clear();

        foreach (var invoice in matches)
        {
            Invoices.Add(new InvoiceRowViewModel(invoice));
        }

        // Drafts are not owed yet, so they stay out of what the summary reports.
        var issued = matches.Where(invoice => invoice.Status != InvoiceStatus.Draft).ToList();

        _shownIssuedTotal = issued.Sum(invoice => invoice.GrandTotal);
        _shownOutstandingTotal = issued.Sum(invoice => invoice.OutstandingAmount);

        OnPropertyChanged(nameof(ListSummary));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static bool Matches(Invoice invoice, string query) =>
        invoice.Number.Contains(query, StringComparison.CurrentCultureIgnoreCase)
        || (invoice.Client?.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false);

    private void BuildStatusOptions()
    {
        var selected = SelectedStatus.Status;

        StatusOptions.Clear();
        StatusOptions.Add(StatusFilterOption.All());

        foreach (var status in Enum.GetValues<InvoiceStatus>())
        {
            StatusOptions.Add(StatusFilterOption.For(status));
        }

        SelectedStatus = StatusOptions.First(option => option.Status == selected);
    }

    private void BuildClientOptions()
    {
        var selectedId = SelectedClient.ClientId;

        var clients = _loaded
            .Where(invoice => invoice.Client is not null)
            .Select(invoice => invoice.Client!)
            .DistinctBy(client => client.Id)
            .OrderBy(client => client.Name);

        ClientOptions.Clear();
        ClientOptions.Add(ClientFilterOption.All());

        foreach (var client in clients)
        {
            ClientOptions.Add(new ClientFilterOption(client.Id, client.Name));
        }

        SelectedClient = ClientOptions.FirstOrDefault(option => option.ClientId == selectedId)
            ?? ClientOptions[0];
    }

    private static string FormatMoney(decimal amount) =>
        string.Create(CultureInfo.CurrentUICulture, $"€{amount:N0}");
}

/// <summary>One entry of the status filter; "All" is the entry with no status.</summary>
public class StatusFilterOption
{
    private StatusFilterOption(InvoiceStatus? status, string label)
    {
        Status = status;
        Label = label;
    }

    public InvoiceStatus? Status { get; }

    public string Label { get; }

    public static StatusFilterOption All() => new(null, LocalizedStrings.Get("Invoices_AnyStatus"));

    public static StatusFilterOption For(InvoiceStatus status) =>
        new(status, LocalizedStrings.Get($"Status_{status}"));

    public bool Matches(Invoice invoice) => Status is null || invoice.Status == Status;
}

/// <summary>One entry of the client filter; "All" is the entry with no client.</summary>
public class ClientFilterOption
{
    public ClientFilterOption(int? clientId, string label)
    {
        ClientId = clientId;
        Label = label;
    }

    public int? ClientId { get; }

    public string Label { get; }

    public static ClientFilterOption All() => new(null, LocalizedStrings.Get("Invoices_AnyClient"));

    public bool Matches(Invoice invoice) => ClientId is null || invoice.ClientId == ClientId;
}
