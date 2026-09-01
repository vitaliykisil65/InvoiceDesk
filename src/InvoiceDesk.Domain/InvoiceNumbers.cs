using System.Globalization;

namespace InvoiceDesk.Domain;

/// <summary>
/// Invoice numbers look like <c>INV-2026-0001</c>: a prefix the user picks, the
/// year the invoice belongs to, and a counter that restarts every January.
/// </summary>
public static class InvoiceNumbers
{
    public const string DefaultPrefix = "INV";

    private const int SequenceDigits = 4;

    public static string Format(string prefix, int year, int sequence) =>
        string.Create(
            CultureInfo.InvariantCulture,
            $"{Normalize(prefix)}-{year:D4}-{sequence.ToString($"D{SequenceDigits}", CultureInfo.InvariantCulture)}");

    /// <summary>
    /// The first free number for the year. Numbers in any other shape are
    /// ignored rather than rejected: the user is free to type their own.
    /// </summary>
    public static string Next(string prefix, int year, IEnumerable<string> existingNumbers)
    {
        var highest = existingNumbers
            .Select(number => SequenceOf(number, prefix, year))
            .DefaultIfEmpty(0)
            .Max();

        return Format(prefix, year, highest + 1);
    }

    /// <summary>The counter inside a number of this year and prefix, or zero.</summary>
    private static int SequenceOf(string number, string prefix, int year)
    {
        var expectedStart = string.Create(CultureInfo.InvariantCulture, $"{Normalize(prefix)}-{year:D4}-");

        if (!number.StartsWith(expectedStart, StringComparison.OrdinalIgnoreCase))
        {
            return 0;
        }

        var tail = number[expectedStart.Length..];

        return int.TryParse(tail, NumberStyles.None, CultureInfo.InvariantCulture, out var sequence)
            ? sequence
            : 0;
    }

    private static string Normalize(string prefix) =>
        string.IsNullOrWhiteSpace(prefix) ? DefaultPrefix : prefix.Trim();
}
