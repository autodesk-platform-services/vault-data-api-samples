using System.Diagnostics;

namespace VaultDataAPISampleApp.Platform
{
    internal sealed class ExternalUriLauncher : IExternalUriLauncher
    {
        public void Open(Uri uri)
        {
            ArgumentNullException.ThrowIfNull(uri);

            var startInfo = new ProcessStartInfo
            {
                FileName = uri.AbsoluteUri,
                UseShellExecute = true
            };

            Process.Start(startInfo);
        }
    }
}
