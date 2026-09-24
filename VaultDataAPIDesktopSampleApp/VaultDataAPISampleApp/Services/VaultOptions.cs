namespace VaultDataAPISampleApp.Services;

public sealed class VaultOptions
{
    public const string SectionName = "Vault";

    public string ApiBaseUri { get; set; } = string.Empty;

    public static bool IsValidApiBaseUri(string? value)
    {
        return Uri.TryCreate(value, UriKind.Relative, out _)
            && value.StartsWith("/", StringComparison.Ordinal)
            && !value.StartsWith("//", StringComparison.Ordinal)
            && !value.Contains('?', StringComparison.Ordinal)
            && !value.Contains('#', StringComparison.Ordinal);
    }
}
