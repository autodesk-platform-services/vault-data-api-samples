using System.Windows;

namespace VaultDataAPISampleApp.Dialogs.Impl
{
    internal sealed class WpfMessageDialogService : IMessageDialogService
    {
        public void ShowInformation(string message)
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                "Vault Data API",
                MessageBoxButton.OK,
                MessageBoxImage.Information);
        }

        public void ShowError(string message)
        {
            MessageBox.Show(
                Application.Current.MainWindow,
                message,
                "Vault Data API",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
        }
    }
}
