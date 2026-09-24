namespace VaultDataAPISampleApp.Http
{
    internal static class VaultApiHttpClient
    {
        public const string Name = "VaultDataApi";

        public static TimeSpan RequestTimeout { get; } = TimeSpan.FromMinutes(1);
    }
}
