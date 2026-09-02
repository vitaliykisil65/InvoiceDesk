using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// Every payment on one screen, next to a form for recording a new one. The
/// invoice picker only offers what can still receive money: a draft has not
/// been sent, so nothing is owed on it yet.
/// </summary>
public partial class PaymentsViewModel : PageViewModel
{
    private static readonly HashSet<string> FormFields =
    [
        nameof(SelectedInvoiceOption), nameof(PaidOn), nameof(Amount)
    ];

    private readonly IPaymentStore _payments;

    private readonly IInvoiceStore _invoices;

    private readonly ConfirmationService _confirmation;

    private List<Payment> _loaded = [];

    /// <summary>Invoice to preselect on the next activation; set by <see cref="Open"/>.</summary>
    private int? _requestedInvoiceId;

    /// <summary>Filling the form must not read as the user editing it.</summary>
    private bool _isFillingForm;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private PaymentRowViewModel? _selectedPayment;

    [ObservableProperty]
    private InvoicePickerOption? _selectedInvoiceOption;

    [ObservableProperty]
    private string _paidOn = string.Empty;

    [ObservableProperty]
    private string _amount = string.Empty;

    [ObservableProperty]
    private string _method = string.Empty;

    [ObservableProperty]
    private string _reference = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public PaymentsViewModel(IPaymentStore payments, IInvoiceStore invoices, ConfirmationService confirmation)
    {
        _payments = payments;
        _invoices = invoices;
        _confirmation = confirmation;
    }

    public override string TitleKey => "Nav_Payments";

    public override string Icon => "";

    public ObservableCollection<PaymentRowViewModel> Payments { get; } = [];

    public ObservableCollection<InvoicePickerOption> InvoiceOptions { get; } = [];

    public string DateHint => LocalizedStrings.Format("Payments_DateHint", CultureText.DatePattern);

    public string ListSummary => LocalizedStrings.Format("Payments_Count", Payments.Count);

    public bool IsEmpty => Payments.Count == 0;

    /// <summary>
    /// Points the form at an invoice, so recording a payment from the invoice
    /// list opens here with the invoice already picked.
    /// </summary>
    public void Open(int? invoiceId) => _requestedInvoiceId = invoiceId;

    public override async Task OnActivatedAsync(CancellationToken cancellationToken = default)
    {
        await LoadInvoiceOptionsAsync(cancellationToken);
        await LoadAsync(cancellationToken);

        StartNewPayment();

        if (_requestedInvoiceId is { } invoiceId)
        {
            SelectedInvoiceOption = InvoiceOptions.FirstOrDefault(option => option.Id == invoiceId);
            _requestedInvoiceId = null;
        }
    }

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        _loaded = [.. await _payments.GetAsync(cancellationToken)];
        ApplyFilter();
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();

        ApplyFilter();
        RefreshValidation();

        OnPropertyChanged(nameof(DateHint));
        OnPropertyChanged(nameof(ListSummary));
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (!_isFillingForm && e.PropertyName is not null && FormFields.Contains(e.PropertyName))
        {
            RefreshValidation();
        }
    }

    [RelayCommand]
    private void NewPayment() => StartNewPayment();

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var payment = new Payment
        {
            InvoiceId = SelectedInvoiceOption!.Id,
            PaidOn = CultureText.ParseDate(PaidOn) ?? DateTime.Today,
            Amount = CultureText.ParseNumber(Amount) ?? 0m,
            Method = Trimmed(Method),
            Reference = Trimmed(Reference)
        };

        try
        {
            IsBusy = true;

            await _payments.AddAsync(payment);
            StatusMessage = LocalizedStrings.Format("Payments_Saved", SelectedInvoiceOption.Label);

            await LoadInvoiceOptionsAsync();
            await LoadAsync();
            StartNewPayment();
        }
        catch (Exception exception)
        {
            // The storage layer's exception types belong to EF Core, which this
            // project deliberately cannot see, so the message is what we show.
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            SaveCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSave() => !IsBusy && Validate().Length == 0;

    [RelayCommand(CanExecute = nameof(CanDelete))]
    private async Task DeleteAsync()
    {
        if (SelectedPayment is null
            || !_confirmation.Ask(LocalizedStrings.Format("Payments_DeleteConfirm", SelectedPayment.InvoiceNumber)))
        {
            return;
        }

        try
        {
            IsBusy = true;

            await _payments.DeleteAsync(SelectedPayment.Id);
            StatusMessage = LocalizedStrings.Get("Payments_DeletedStatus");

            SelectedPayment = null;

            await LoadInvoiceOptionsAsync();
            await LoadAsync();
        }
        catch (Exception exception)
        {
            StatusMessage = exception.Message;
        }
        finally
        {
            IsBusy = false;
            DeleteCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanDelete() => !IsBusy && SelectedPayment is not null;

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnSelectedPaymentChanged(PaymentRowViewModel? value) => DeleteCommand.NotifyCanExecuteChanged();

    private async Task LoadInvoiceOptionsAsync(CancellationToken cancellationToken = default)
    {
        var selectedId = SelectedInvoiceOption?.Id;

        // A draft has not been sent, so nothing is owed on it yet.
        var invoices = (await _invoices.GetAsync(cancellationToken))
            .Where(invoice => invoice.Status != InvoiceStatus.Draft)
            .OrderByDescending(invoice => invoice.IssuedOn);

        InvoiceOptions.Clear();

        foreach (var invoice in invoices)
        {
            InvoiceOptions.Add(new InvoicePickerOption(invoice));
        }

        SelectedInvoiceOption = InvoiceOptions.FirstOrDefault(option => option.Id == selectedId);
    }

    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        var matches = _loaded.Where(payment => query.Length == 0 || Matches(payment, query));

        Payments.Clear();

        foreach (var payment in matches)
        {
            Payments.Add(new PaymentRowViewModel(payment));
        }

        OnPropertyChanged(nameof(ListSummary));
        OnPropertyChanged(nameof(IsEmpty));
    }

    private static bool Matches(Payment payment, string query) =>
        (payment.Invoice?.Number.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false)
        || (payment.Invoice?.Client?.Name.Contains(query, StringComparison.CurrentCultureIgnoreCase) ?? false);

    private void StartNewPayment()
    {
        _isFillingForm = true;

        SelectedInvoiceOption = null;
        PaidOn = CultureText.FormatDate(DateTime.Today);
        Amount = string.Empty;
        Method = string.Empty;
        Reference = string.Empty;

        _isFillingForm = false;

        ValidationMessage = string.Empty;
        StatusMessage = string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
    }

    private void RefreshValidation()
    {
        ValidationMessage = Validate();
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Empty when the form can be saved, otherwise the reason it cannot.</summary>
    private string Validate()
    {
        if (SelectedInvoiceOption is null)
        {
            return LocalizedStrings.Get("Payments_InvoiceRequired");
        }

        if (CultureText.ParseDate(PaidOn) is null)
        {
            return LocalizedStrings.Get("Payments_DateInvalid");
        }

        if (CultureText.ParseNumber(Amount) is null or <= 0m)
        {
            return LocalizedStrings.Get("Payments_AmountInvalid");
        }

        return string.Empty;
    }

    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}