using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// The price list and its editor, side by side, the way the clients screen
/// works: picking an entry on the left loads it into the form on the right, and
/// "New service" clears the form. Nothing reaches the database until the user
/// saves.
/// </summary>
public partial class ProductsViewModel : PageViewModel
{
    private static readonly HashSet<string> FormFields =
    [
        nameof(Name), nameof(Description), nameof(Unit), nameof(UnitPrice), nameof(TaxRate)
    ];

    private readonly IProductStore _store;

    private List<Product> _loaded = [];

    /// <summary>Id of the product in the form; zero while a new one is being typed.</summary>
    private int _editingId;

    private bool _editingIsArchived;

    /// <summary>Selecting an entry fills the form, and that must not look like an edit.</summary>
    private bool _isFillingForm;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showArchived;

    [ObservableProperty]
    private ProductListItemViewModel? _selectedProduct;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _description = string.Empty;

    [ObservableProperty]
    private string _unit = string.Empty;

    [ObservableProperty]
    private string _unitPrice = string.Empty;

    [ObservableProperty]
    private string _taxRate = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ProductsViewModel(IProductStore store)
    {
        _store = store;
    }

    public override string TitleKey => "Nav_Services";

    public override string Icon => "";

    public ObservableCollection<ProductListItemViewModel> Products { get; } = [];

    public bool IsNewProduct => _editingId == 0;

    public string EditorTitle => LocalizedStrings.Get(IsNewProduct ? "Products_NewTitle" : "Products_EditTitle");

    public string ArchiveActionText =>
        LocalizedStrings.Get(_editingIsArchived ? "Products_Restore" : "Products_Archive");

    public string ListSummary => LocalizedStrings.Format("Products_Count", Products.Count);

    /// <summary>Reloads the list whenever the shell navigates here.</summary>
    public override Task OnActivatedAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(cancellationToken);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var editingId = _editingId;

        _loaded = [.. await _store.GetAsync(ShowArchived, cancellationToken)];
        ApplyFilter();

        // Keep the user where they were: reselect whatever the editor had open.
        SelectedProduct = Products.FirstOrDefault(item => item.Id == editingId);

        if (SelectedProduct is null && editingId != 0)
        {
            StartNewProduct();
        }
    }

    public override void OnLanguageChanged()
    {
        base.OnLanguageChanged();

        ApplyFilter();
        RefreshValidation();

        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(ArchiveActionText));
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

    partial void OnSearchTextChanged(string value) => ApplyFilter();

    partial void OnShowArchivedChanged(bool value) => _ = LoadAsync();

    partial void OnSelectedProductChanged(ProductListItemViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        var product = _loaded.FirstOrDefault(candidate => candidate.Id == value.Id);

        if (product is not null)
        {
            FillForm(product);
        }
    }

    [RelayCommand]
    private void NewProduct()
    {
        SelectedProduct = null;
        StartNewProduct();
    }

    [RelayCommand]
    private void Cancel()
    {
        var product = _loaded.FirstOrDefault(candidate => candidate.Id == _editingId);

        if (product is null)
        {
            StartNewProduct();
            return;
        }

        FillForm(product);
        StatusMessage = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var product = new Product
        {
            Id = _editingId,
            Name = Name.Trim(),
            Description = Trimmed(Description),
            Unit = Unit.Trim(),
            UnitPrice = CultureText.ParseNumber(UnitPrice) ?? 0m,
            TaxRate = CultureText.ParseNumber(TaxRate) ?? 0m
        };

        try
        {
            IsBusy = true;

            _editingId = await _store.SaveAsync(product);
            StatusMessage = LocalizedStrings.Format("Products_Saved", product.Name);

            await LoadAsync();
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
            ToggleArchiveCommand.NotifyCanExecuteChanged();
        }
    }

    private bool CanSave() => !IsBusy && Validate().Length == 0;

    [RelayCommand(CanExecute = nameof(CanArchive))]
    private async Task ToggleArchiveAsync()
    {
        var restoring = _editingIsArchived;

        await _store.SetArchivedAsync(_editingId, !restoring);
        StatusMessage = LocalizedStrings.Get(restoring ? "Products_RestoredStatus" : "Products_ArchivedStatus");

        // An archived entry drops out of the list unless archived ones are shown,
        // so the editor moves on to a blank form rather than a vanished record.
        if (!restoring && !ShowArchived)
        {
            StartNewProduct();
        }
        else
        {
            _editingIsArchived = !restoring;
            OnPropertyChanged(nameof(ArchiveActionText));
        }

        await LoadAsync();
    }

    private bool CanArchive() => _editingId != 0 && !IsBusy;

    private void ApplyFilter()
    {
        var query = SearchText.Trim();

        var matches = _loaded
            .Where(product => query.Length == 0 || Matches(product, query))
            .Select(product => new ProductListItemViewModel(product));

        Products.Clear();

        foreach (var match in matches)
        {
            Products.Add(match);
        }

        OnPropertyChanged(nameof(ListSummary));
    }

    private static bool Matches(Product product, string query) =>
        Contains(product.Name, query) || Contains(product.Description, query);

    private static bool Contains(string? value, string query) =>
        value is not null && value.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private void StartNewProduct()
    {
        FillForm(new Product());
        StatusMessage = string.Empty;
    }

    private void FillForm(Product product)
    {
        _isFillingForm = true;

        _editingId = product.Id;
        _editingIsArchived = product.IsArchived;

        Name = product.Name;
        Description = product.Description ?? string.Empty;
        Unit = product.Unit;
        UnitPrice = CultureText.FormatNumber(product.UnitPrice);
        TaxRate = CultureText.FormatNumber(product.TaxRate);

        _isFillingForm = false;

        ValidationMessage = string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
        ToggleArchiveCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(IsNewProduct));
        OnPropertyChanged(nameof(EditorTitle));
        OnPropertyChanged(nameof(ArchiveActionText));
    }

    private void RefreshValidation()
    {
        ValidationMessage = Validate();
        SaveCommand.NotifyCanExecuteChanged();
    }

    /// <summary>Empty when the form can be saved, otherwise the reason it cannot.</summary>
    private string Validate()
    {
        if (string.IsNullOrWhiteSpace(Name))
        {
            return LocalizedStrings.Get("Products_NameRequired");
        }

        if (string.IsNullOrWhiteSpace(Unit))
        {
            return LocalizedStrings.Get("Products_UnitRequired");
        }

        if (CultureText.ParseNumber(UnitPrice) is null or < 0m)
        {
            return LocalizedStrings.Get("Products_PriceInvalid");
        }

        if (CultureText.ParseNumber(TaxRate) is null or < 0m or > 100m)
        {
            return LocalizedStrings.Get("Products_TaxRateInvalid");
        }

        return string.Empty;
    }

    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
