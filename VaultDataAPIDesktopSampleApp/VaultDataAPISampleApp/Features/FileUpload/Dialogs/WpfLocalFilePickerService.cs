using Microsoft.Win32;

namespace VaultDataAPISampleApp.Features.FileUpload.Dialogs
{
    internal sealed class WpfLocalFilePickerService : ILocalFilePickerService
    {
        public string? SelectFile()
        {
            var dialog = new OpenFileDialog
            {
                CheckFileExists = true,
                Multiselect = false,
                Title = "Select a file to upload"
            };

            return dialog.ShowDialog() == true ? dialog.FileName : null;
        }
    }
}
