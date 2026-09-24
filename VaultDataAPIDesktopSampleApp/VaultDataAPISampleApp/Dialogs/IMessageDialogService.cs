namespace VaultDataAPISampleApp.Dialogs
{
    public interface IMessageDialogService
    {
        void ShowInformation(string message);

        void ShowError(string message);
    }
}
