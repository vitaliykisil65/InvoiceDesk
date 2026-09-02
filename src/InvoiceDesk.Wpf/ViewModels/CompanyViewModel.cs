using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Wpf.Localization;
using InvoiceDesk.Wpf.Services;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// The seller's own details, printed on every invoice and used as the defaults
/// a new invoice starts from: its currency, payment term and number prefix.
/// </summary>
public partial class CompanyViewModel : PageViewModel, IDataErrorInfo
{
    private static readonly HashSet<string> FormFields =
    [
        nameof(Name), nameof(Email), nameof(DefaultCurrency),
        nameof(PaymentTermDays), nameof(InvoiceNumberPrefix)
    ];

    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _taxNumber = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _bank = string.Empty;

    [ObservableProperty]
    private string _iban = string.Empty;

    [ObservableProperty]
    private string _defaultCurrency = "EUR";

    [ObservableProperty]
    private string _paymentTermDays = "14";

    [ObservableProperty]
    private string _invoiceNumberPrefix = "INV";

    [ObservableProperty]
    private string _invoiceFooter = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    public CompanyViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        LoadFromSettings();
    }

    public override string TitleKey => "Nav_Company";

    public override string Icon => "";

    /// <summary>Required by <see cref="IDataErrorInfo"/>; the form has no whole-object error.</summary>
    public string Error => string.Empty;

    /// <summary>Per-field message a bound control shows as a red border and tooltip.</summary>
    public string this[string columnName] => columnName switch
    {
        nameof(Name) => string.IsNullOrWhiteSpace(Name)
            ? LocalizedStrings.Get("Company_NameRequired")
            : string.Empty,
        nameof(Email) => Email.Length > 0 && (!Email.Contains('@') || Email.EndsWith('@'))
            ? LocalizedStrings.Get("Company_EmailInvalid")
            : string.Empty,
        nameof(DefaultCurrency) => IsCurrencyCode(DefaultCurrency)
            ? string.Empty
            : LocalizedStrings.Get("Company_CurrencyInvalid"),
        nameof(PaymentTermDays) => CultureText.ParseNumber(PaymentTermDays) is > 0m
            ? string.Empty
            : LocalizedStrings.Get("Company_PaymentTermInvalid"),
        nameof(InvoiceNumberPrefix) => string.IsNullOrWhiteSpace(InvoiceNumberPrefix)
            ? LocalizedStrings.Get("Company_PrefixRequired")
            : string.Empty,
        _ => string.Empty
    };

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save()
    {
        var stored = _settingsService.Current.Clone();
        stored.Company = new CompanyProfile
        {
            Name = Name.Trim(),
            Address = Address.Trim(),
            TaxNumber = TaxNumber.Trim(),
            Email = Email.Trim(),
            Phone = Phone.Trim(),
            Bank = Bank.Trim(),
            Iban = Iban.Trim(),
            DefaultCurrency = DefaultCurrency.Trim().ToUpperInvariant(),
            PaymentTermDays = (int)(CultureText.ParseNumber(PaymentTermDays) ?? 14m),
            InvoiceNumberPrefix = InvoiceNumberPrefix.Trim(),
            InvoiceFooter = InvoiceFooter.Trim()
        };

        _settingsService.Save(stored);
        StatusMessage = LocalizedStrings.Get("Company_Saved");
    }

    private bool CanSave() => Validate().Length == 0;

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();
        RefreshValidation();
        StatusMessage = string.Empty;
    }

    protected override void OnPropertyChanged(PropertyChangedEventArgs e)
    {
        base.OnPropertyChanged(e);

        if (e.PropertyName is not null && FormFields.Contains(e.PropertyName))
        {
            RefreshValidation();
        }
    }

    private void LoadFromSettings()
    {
        var company = _settingsService.Current.Company;

        Name = company.Name;
        Address = company.Address;
        TaxNumber = company.TaxNumber;
        Email = company.Email;
        Phone = company.Phone;
        Bank = company.Bank;
        Iban = company.Iban;
        DefaultCurrency = company.DefaultCurrency;
        PaymentTermDays = CultureText.FormatNumber(company.PaymentTermDays);
        InvoiceNumberPrefix = company.InvoiceNumberPrefix;
        InvoiceFooter = company.InvoiceFooter;
    }

    private void RefreshValidation()
    {
        ValidationMessage = Validate();
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Empty when the form can be saved, otherwise the first reason it cannot.</summary>
    private string Validate()
    {
        string[] fieldsInOrder =
        [
            nameof(Name), nameof(InvoiceNumberPrefix), nameof(DefaultCurrency),
            nameof(PaymentTermDays), nameof(Email)
        ];

        return fieldsInOrder.Select(field => this[field]).FirstOrDefault(message => message.Length > 0)
               ?? string.Empty;
    }

    private static bool IsCurrencyCode(string value) =>
        value.Trim().Length == 3 && value.Trim().All(char.IsLetter);
}
