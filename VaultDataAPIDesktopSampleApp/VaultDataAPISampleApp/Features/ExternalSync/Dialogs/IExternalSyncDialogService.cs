namespace VaultDataAPISampleApp.Features.ExternalSync.Dialogs
{
    public interface IExternalSyncDialogService
    {
        bool ConfirmCreateTask(string configId, string workflowType);

        bool ConfirmDeleteTask(string taskId);
    }
}
