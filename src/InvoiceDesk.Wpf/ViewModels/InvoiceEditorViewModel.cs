using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Domain;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;
using Serilog;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// The invoice itself: who it goes to, what is on it and what it comes to. The
/// page is opened from the invoice list or from the shell's "New invoice"
/// button, and it is the one screen that writes lines to the database.
///
/// Only drafts can be edited. Once an invoice has been sent the client has seen
/// it, so it opens read-only and the totals it was issued for stay as they were.
/// </summary>
public partial class InvoiceEditorViewModel : PageViewModel
{
    private static readonly HashSet<string> FormFields =
    [
        nameof(Number), nameof(SelectedClient), nameof(IssuedOn), nameof(DueOn),
        nameof(DiscountPercent), nameof(Notes)
    ];

    private readonly IInvoiceStore _invoices;

    private readonly IClientStore _clients;

    private readonly IProductStore _products;

    private readonly SettingsService _settings;

    private readonly NavigationService _navigation;

    /// <summary>The stored invoice being edited; a new one until it is saved.</summary>
    private Invoice _editing = new();

    /// <summary>Which invoice to open on the next activation; null means a new one.</summary>
    private int? _requestedInvoiceId;

    /// <summary>Filling the form must not read as the user editing it.</summary>
    private bool _isFillingForm;

    [ObservableProperty]
    private string _number = string.Empty;

    [ObservableProperty]
    private Client? _selectedClient;

    [ObservableProperty]
    private string _issuedOn = string.Empty;

    [ObservableProperty]
    private string _dueOn = string.Empty;

    [ObservableProperty]
    private string _discountPercent = "0";

    [ObservableProperty]
    private string _notes = string.Empty;

    /// <summary>The price list entry the "Add" button next to it would bill for.</summary>
    [ObservableProperty]
    private Product? _selectedProduct;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public InvoiceEditorViewModel(
        IInvoiceStore invoices,
        IClientStore clients,
        IProductStore products,
        SettingsService settings,
        NavigationService navigation)
    {
        _invoices = invoices;
        _clients = clients;
        _products = products;
        _settings = settings;
        _navigation = navigation;

        Lines.CollectionChanged += OnLinesChanged;
    }

    public override string TitleKey => "InvoiceEditor_Title";

    public override string Icon => "";

    public ObservableCollection<InvoiceLineViewModel> Lines { get; } = [];

    public ObservableCollection<Client> Clients { get; } = [];

    public ObservableCollection<Product> Products { get; } = [];

    public bool IsNewInvoice => _editing.Id == 0;

    /// <summary>A sent invoice is a document somebody outside the company holds.</summary>
    public bool IsEditable => _editing.Status == InvoiceStatus.Draft;

    public string EditorTitle => IsNewInvoice
        ? LocalizedStrings.Get("InvoiceEditor_NewTitle")
        : LocalizedStrings.Format("InvoiceEditor_EditTitle", _editing.Number);

    public string StatusText => LocalizedStrings.Get($"Status_{_editing.Status}");

    public string DateHint => LocalizedStrings.Format("InvoiceEditor_DateHint", CultureText.DatePattern);

    public string NetText => CultureText.FormatMoney(Totals().NetTotal, _editing.Currency);

    public string DiscountText => CultureText.FormatMoney(-Totals().DiscountAmount, _editing.Currency);

    public string TaxText => CultureText.FormatMoney(Totals().TaxTotal, _editing.Currency);

    public string GrandTotalText => CultureText.FormatMoney(Totals().GrandTotal, _editing.Currency);

    public string PaidText => CultureText.FormatMoney(_editing.PaidAmount, _editing.Currency);

    public string OutstandingText =>
        CultureText.FormatMoney(Totals().GrandTotal - _editing.PaidAmount, _editing.Currency);

    /// <summary>
    /// Points the editor at an invoice, or at a blank one when the id is null.
    /// The data is read when the shell activates the page, not here, so the
    /// caller can navigate without awaiting anything.
    /// </summary>
    public void Open(int? invoiceId)
    {
        _requestedInvoiceId = invoiceId;
        StatusMessage = string.Empty;
    }

    public override async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        await LoadPickersAsync(cancellationToken);

        var invoice = _requestedInvoiceId is { } invoiceId
            ? await _invoices.GetByIdAsync(invoiceId, cancellationToken)
            : null;

        FillForm(invoice ?? await NewInvoiceAsync(cancellationToken));
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();

        foreach (var line in Lines)
        {
            line.OnLanguageChanged();
        }

        RefreshValidation();
        RefreshTotals();

        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(StatusText));
        OnPropertyChanged(nameof(DateHint));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (_isFillingForm || e.PropertyName is null || !FormFields.Contains(e.PropertyName))
        {
            return;
        }

        RefreshValidation();

        if (e.PropertyName == nameof(DiscountPercent))
        {
            RefreshTotals();
        }
    }

    partial void OnSelectedProductChanged(Product? value) =>
        AddFromPriceListCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void AddLine()
    {
        Lines.Add(new InvoiceLineViewModel { Currency = _editing.Currency });
        StatusMessage = string.Empty;
    }

    /// <summary>Bills for a price list entry, at the price the price list has today.</summary>
    [RelayCommand(CanExecute = nameof(CanAddFromPriceList))]
    private void AddFromPriceList()
    {
        if (SelectedProduct is null)
        {
            return;
        }

        Lines.Add(new InvoiceLineViewModel(SelectedProduct) { Currency = _editing.Currency });

        SelectedProduct = null;
        StatusMessage = string.Empty;
    }

    private bool CanAddFromPriceList() => SelectedProduct is not null;

    [RelayCommand]
    private void RemoveLine(InvoiceLineViewModel? line)
    {
        if (line is not null)
        {
            Lines.Remove(line);
        }
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var invoice = BuildInvoice();

        try
        {
            IsBusy = true;

            var invoiceId = await _invoices.SaveAsync(invoice);
            StatusMessage = LocalizedStrings.Format("InvoiceEditor_Saved", invoice.Number);

            // Read back what was stored: the store settles the id, and the
            // status that the payments and the due date imply.
            _requestedInvoiceId = invoiceId;
            FillForm(await _invoices.GetByIdAsync(invoiceId) ?? invoice);
        }
        catch (Exception exception)
        {
            // The storage layer's exception types belong to EF Core, which this
            // project deliberately cannot see, so the message is what we show.
            Log.Error(exception, "Failed to save invoice {Number}", invoice.Number);
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSave() => !IsBusy && IsEditable && Validate().Length == 0;

    [RelayCommand]
    private void Back() => _navigation.GoBack();

    /// <summary>
    /// Archived clients and services stay out of the pickers; an invoice that
    /// already bills for one keeps it, which <see cref="FillForm"/> handles.
    /// </summary>
    private async Task LoadPickersAsync(CancellationToken cancellationToken)
    {
        var clients = await _clients.GetAsync(false, cancellationToken);
        var products = await _products.GetAsync(false, cancellationToken);

        Clients.Clear();

        foreach (var client in clients)
        {
            Clients.Add(client);
        }

        Products.Clear();

        foreach (var product in products)
        {
            Products.Add(product);
        }
    }

    /// <summary>A draft with the next free number, dated today and due per the company's payment term.</summary>
    private async Task<Invoice> NewInvoiceAsync(CancellationToken cancellationToken)
    {
        var today = DateTime.Today;
        var company = _settings.Current.Company;

        var prefix = string.IsNullOrWhiteSpace(company.InvoiceNumberPrefix)
            ? InvoiceNumbers.DefaultPrefix
            : company.InvoiceNumberPrefix;

        return new Invoice
        {
            Number = await _invoices.GetNextNumberAsync(prefix, today.Year, cancellationToken),
            IssuedOn = today,
            DueOn = today.AddDays(company.PaymentTermDays),
            Currency = company.DefaultCurrency,
            Status = InvoiceStatus.Draft
        };
    }

    private void FillForm(Invoice invoice)
    {
        _isFillingForm = true;
        _editing = invoice;

        Number = invoice.Number;
        IssuedOn = CultureText.FormatDate(invoice.IssuedOn);
        DueOn = CultureText.FormatDate(invoice.DueOn);
        DiscountPercent = CultureText.FormatNumber(invoice.DiscountPercent);
        Notes = invoice.Notes ?? string.Empty;
        SelectedProduct = null;

        // The invoice may be for a client who has since been archived, and the
        // picker has to carry them anyway or the form loses who it is for.
        if (invoice.Client is not null && Clients.All(client => client.Id != invoice.Client.Id))
        {
            Clients.Add(invoice.Client);
        }

        SelectedClient = Clients.FirstOrDefault(client => client.Id == invoice.ClientId);

        Lines.Clear();

        foreach (var line in invoice.Lines)
        {
            Lines.Add(new InvoiceLineViewModel(line) { Currency = invoice.Currency });
        }

        _isFillingForm = false;

        ValidationMessage = string.Empty;
        RefreshTotals();

        OnPropertyChanged(nameof(IsNewInvoice));
        OnPropertyChanged(nameof(IsEditable));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(StatusText));

        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>The invoice the form currently describes, ready to be stored.</summary>
    private Invoice BuildInvoice() => new()
    {
        Id = _editing.Id,
        Number = Number.Trim(),
        ClientId = SelectedClient?.Id ?? 0,
        IssuedOn = CultureText.ParseDate(IssuedOn) ?? DateTime.Today,
        DueOn = CultureText.ParseDate(DueOn) ?? DateTime.Today,
        Currency = _editing.Currency,
        DiscountPercent = CultureText.ParseNumber(DiscountPercent) ?? 0m,
        Notes = string.IsNullOrWhiteSpace(Notes) ? null : Notes.Trim(),
        Status = _editing.Status,
        Lines = [.. Lines.Select(line => line.ToLine())]
    };

    /// <summary>
    /// What the form comes to right now. The money rules live in the domain, so
    /// the editor totals an invoice rather than adding anything up itself.
    /// </summary>
    private Invoice Totals() => BuildInvoice();

    private void OnLinesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        foreach (var line in e.OldItems?.OfType<InvoiceLineViewModel>() ?? [])
        {
            line.PropertyChanged -= OnLinePropertyChanged;
        }

        foreach (var line in e.NewItems?.OfType<InvoiceLineViewModel>() ?? [])
        {
            line.PropertyChanged += OnLinePropertyChanged;
        }

        if (!_isFillingForm)
        {
            RefreshValidation();
            RefreshTotals();
        }
    }

    private void OnLinePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        // The row recomputes its own total; the invoice only cares about what
        // was typed into it.
        if (e.PropertyName == nameof(InvoiceLineViewModel.NetText))
        {
            return;
        }

        RefreshValidation();
        RefreshTotals();
    }

    private void RefreshTotals()
    {
        OnPropertyChanged(nameof(NetText));
        OnPropertyChanged(nameof(DiscountText));
        OnPropertyChanged(nameof(TaxText));
        OnPropertyChanged(nameof(GrandTotalText));
        OnPropertyChanged(nameof(PaidText));
        OnPropertyChanged(nameof(OutstandingText));
    }

    private void RefreshValidation()
    {
        ValidationMessage = Validate();
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Empty when the invoice can be saved, otherwise the reason it cannot.</summary>
    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(Number))
        {
            return LocalizedStrings.Get("InvoiceEditor_NumberRequired");
        }

        if (SelectedClient is null)
        {
            return LocalizedStrings.Get("InvoiceEditor_ClientRequired");
        }

        if (CultureText.ParseDate(IssuedOn) is not { } issued)
        {
            return LocalizedStrings.Get("InvoiceEditor_IssuedInvalid");
        }

        if (CultureText.ParseDate(DueOn) is not { } due)
        {
            return LocalizedStrings.Get("InvoiceEditor_DueInvalid");
        }

        if (due < issued)
        {
            return LocalizedStrings.Get("InvoiceEditor_DueBeforeIssued");
        }

        if (CultureText.ParseNumber(DiscountPercent) is null or < 0m or > 100m)
        {
            return LocalizedStrings.Get("InvoiceEditor_DiscountInvalid");
        }

        if (Lines.Count == 0)
        {
            return LocalizedStrings.Get("InvoiceEditor_LinesRequired");
        }

        var invalid = Lines
            .Select((line, index) => (Line: line, Number: index + 1))
            .FirstOrDefault(entry => !entry.Line.IsValid);

        return invalid.Line is null
            ? string.Empty
            : LocalizedStrings.Format("InvoiceEditor_LineInvalid", invalid.Number);
    }
}
