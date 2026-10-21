using System.Text.Json;

namespace TheBuilder.AIImageDisclosure.Media;

/// <summary>
/// Reads and writes values of the package's dropdown properties in Umbraco's storage format.
/// </summary>
/// <remarks>
/// Umbraco's dropdown stores its selection as a JSON array. A plain string is sent to the editor unchanged
/// and comes back empty on the next save, which would clear the disclosure. Older package versions stored
/// plain strings, so reads accept both forms.
/// </remarks>
internal static class DropdownValue
{
    /// <summary>Returns the stored form of a single selected value, for example <c>["generated"]</c>.</summary>
    public static string Encode(string value) => JsonSerializer.Serialize(new[] { value });

    /// <summary>Returns the selected value of a stored dropdown value in either form, or null when none is selected.</summary>
    public static string? Decode(string? stored)
    {
        if (string.IsNullOrWhiteSpace(stored))
        {
            return null;
        }

        if (stored.TrimStart().StartsWith('[') is false)
        {
            return stored;
        }

        try
        {
            return JsonSerializer.Deserialize<string[]>(stored)?.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value));
        }
        catch (JsonException)
        {
            return stored;
        }
    }
}
