using System.Windows;

namespace VaultDataAPISampleApp.Features.ExternalSync.Dialogs
{
    internal sealed class WpfExternalSyncDialogService
        : IExternalSyncDialogService
    {
        public bool ConfirmCreateTask(string configId, string workflowType)
        {
            var dialog = new CreateTaskConfirmWindow(configId, workflowType)
            {
                Owner = Application.Current.MainWindow
            };

            return dialog.ShowDialog() == true;
        }

        public bool ConfirmDeleteTask(string taskId)
        {
            MessageBoxResult result = MessageBox.Show(
                Application.Current.MainWindow,
                $"Are you sure you want to delete task '{taskId}'?",
                "Confirm Delete",
                MessageBoxButton.YesNo,
                MessageBoxImage.Warning);

            return result == MessageBoxResult.Yes;
        }
    }
}
