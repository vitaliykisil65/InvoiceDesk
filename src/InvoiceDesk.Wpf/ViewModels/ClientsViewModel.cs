using System.Collections.ObjectModel;
using System.ComponentModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using InvoiceDesk.Domain.Abstractions;
using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;
using Serilog;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>
/// The client list and its editor, side by side: picking a client on the left
/// loads it into the form on the right, and "New client" clears the form for a
/// fresh one. Nothing reaches the database until the user saves.
/// </summary>
public partial class ClientsViewModel : PageViewModel
{
    private static readonly HashSet<string> FormFields =
    [
        nameof(Name), nameof(ContactPerson), nameof(Email),
        nameof(Phone), nameof(Address), nameof(TaxNumber), nameof(Notes)
    ];

    private readonly IClientStore _store;

    private List<Client> _loaded = [];

    /// <summary>Id of the client in the form; zero while a new one is being typed.</summary>
    private int _editingId;

    private bool _editingIsArchived;

    /// <summary>Selecting a client fills the form, and that must not look like an edit.</summary>
    private bool _isFillingForm;

    [ObservableProperty]
    private string _searchText = string.Empty;

    [ObservableProperty]
    private bool _showArchived;

    [ObservableProperty]
    private ClientListItemViewModel? _selectedClient;

    [ObservableProperty]
    private string _name = string.Empty;

    [ObservableProperty]
    private string _contactPerson = string.Empty;

    [ObservableProperty]
    private string _email = string.Empty;

    [ObservableProperty]
    private string _phone = string.Empty;

    [ObservableProperty]
    private string _address = string.Empty;

    [ObservableProperty]
    private string _taxNumber = string.Empty;

    [ObservableProperty]
    private string _notes = string.Empty;

    [ObservableProperty]
    private string _validationMessage = string.Empty;

    [ObservableProperty]
    private string _statusMessage = string.Empty;

    [ObservableProperty]
    private bool _isBusy;

    public ClientsViewModel(IClientStore store)
    {
        _store = store;
    }

    public override string TitleKey => "Nav_Clients";

    public override string Icon => "";

    public ObservableCollection<ClientListItemViewModel> Clients { get; } = [];

    public bool IsNewClient => _editingId == 0;

    public string EditorTitle => LocalizedStrings.Get(IsNewClient ? "Clients_NewTitle" : "Clients_EditTitle");

    public string ArchiveActionText =>
        LocalizedStrings.Get(_editingIsArchived ? "Clients_Restore" : "Clients_Archive");

    public string ListSummary => LocalizedStrings.Format("Clients_Count", Clients.Count);

    /// <summary>Reloads the list whenever the shell navigates here.</summary>
    public override Task OnActivatedAsync(CancellationToken cancellationToken = default) =>
        LoadAsync(cancellationToken);

    public async Task LoadAsync(CancellationToken cancellationToken = default)
    {
        var editingId = _editingId;

        _loaded = [.. await _store.GetAsync(ShowArchived, cancellationToken)];
        ApplyFilter();

        // Keep the user where they were: reselect whatever the editor had open.
        SelectedClient = Clients.FirstOrDefault(item => item.Id == editingId);

        if (SelectedClient is null && editingId != 0)
        {
            StartNewClient();
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

    partial void OnSelectedClientChanged(ClientListItemViewModel? value)
    {
        if (value is null)
        {
            return;
        }

        var client = _loaded.FirstOrDefault(candidate => candidate.Id == value.Id);

        if (client is not null)
        {
            FillForm(client);
        }
    }

    [RelayCommand]
    private void NewClient()
    {
        SelectedClient = null;
        StartNewClient();
    }

    [RelayCommand]
    private void Cancel()
    {
        var client = _loaded.FirstOrDefault(candidate => candidate.Id == _editingId);

        if (client is null)
        {
            StartNewClient();
            return;
        }

        FillForm(client);
        StatusMessage = string.Empty;
    }

    [RelayCommand(CanExecute = nameof(CanSave))]
    private async Task SaveAsync()
    {
        var client = new Client
        {
            Id = _editingId,
            Name = Name.Trim(),
            ContactPerson = Trimmed(ContactPerson),
            Email = Trimmed(Email),
            Phone = Trimmed(Phone),
            Address = Trimmed(Address),
            TaxNumber = Trimmed(TaxNumber),
            Notes = Trimmed(Notes)
        };

        try
        {
            IsBusy = true;

            _editingId = await _store.SaveAsync(client);
            StatusMessage = LocalizedStrings.Format("Clients_Saved", client.Name);

            await LoadAsync();
        }
        catch (Exception exception)
        {
            // The storage layer's exception types belong to EF Core, which this
            // project deliberately cannot see, so the message is what we show.
            Log.Error(exception, "Failed to save client {Name}", client.Name);
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
        StatusMessage = LocalizedStrings.Get(restoring ? "Clients_RestoredStatus" : "Clients_ArchivedStatus");

        // An archived client drops out of the list unless archived ones are shown,
        // so the editor moves on to a blank form rather than a vanished record.
        if (!restoring && !ShowArchived)
        {
            StartNewClient();
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
            .Where(client => query.Length == 0 || Matches(client, query))
            .Select(client => new ClientListItemViewModel(client));

        Clients.Clear();

        foreach (var match in matches)
        {
            Clients.Add(match);
        }

        OnPropertyChanged(nameof(ListSummary));
    }

    private static bool Matches(Client client, string query) =>
        Contains(client.Name, query)
        || Contains(client.Email, query)
        || Contains(client.ContactPerson, query)
        || Contains(client.TaxNumber, query);

    private static bool Contains(string? value, string query) =>
        value is not null && value.Contains(query, StringComparison.CurrentCultureIgnoreCase);

    private void StartNewClient()
    {
        FillForm(new Client());
        StatusMessage = string.Empty;
    }

    private void FillForm(Client client)
    {
        _isFillingForm = true;

        _editingId = client.Id;
        _editingIsArchived = client.IsArchived;

        Name = client.Name;
        ContactPerson = client.ContactPerson ?? string.Empty;
        Email = client.Email ?? string.Empty;
        Phone = client.Phone ?? string.Empty;
        Address = client.Address ?? string.Empty;
        TaxNumber = client.TaxNumber ?? string.Empty;
        Notes = client.Notes ?? string.Empty;

        _isFillingForm = false;

        ValidationMessage = string.Empty;
        SaveCommand.NotifyCanExecuteChanged();
        ToggleArchiveCommand.NotifyCanExecuteChanged();

        OnPropertyChanged(nameof(IsNewClient));
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
            return LocalizedStrings.Get("Clients_NameRequired");
        }

        if (Email.Length > 0 && (!Email.Contains('@') || Email.EndsWith('@')))
        {
            return LocalizedStrings.Get("Clients_EmailInvalid");
        }

        return string.Empty;
    }

    private static string? Trimmed(string value) =>
        string.IsNullOrWhiteSpace(value) ? null : value.Trim();
}
