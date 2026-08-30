using InvoiceDesk.Domain.Entities;
using InvoiceDesk.Wpf.Localization;

namespace InvoiceDesk.Wpf.ViewModels;

/// <summary>Read-only projection of a client for the list on the left.</summary>
public class ClientListItemViewModel
{
    public ClientListItemViewModel(Client client)
    {
        Id = client.Id;
        Name = client.Name;
        IsArchived = client.IsArchived;
        Initials = BuildInitials(client.Name);

        Subtitle = client.IsArchived
            ? LocalizedStrings.Get("Clients_ArchivedTag")
            : FirstNotEmpty(client.Email, client.ContactPerson, client.Phone);
    }

    public int Id { get; }

    public string Name { get; }

    public string Subtitle { get; }

    public string Initials { get; }

    public bool IsArchived { get; }

    /// <summary>Up to two letters for the avatar circle: "Blue Harbor" gives "BH".</summary>
    private static string BuildInitials(string name)
    {
        var words = name.Split(' ', StringSplitOptions.RemoveEmptyEntries)
            .Where(word => char.IsLetterOrDigit(word[0]))
            .Take(2)
            .Select(word => char.ToUpperInvariant(word[0]));

        var initials = string.Concat(words);

        return initials.Length == 0 ? "?" : initials;
    }

    private static string FirstNotEmpty(params string?[] candidates) =>
        candidates.FirstOrDefault(candidate => !string.IsNullOrWhiteSpace(candidate)) ?? string.Empty;
}
