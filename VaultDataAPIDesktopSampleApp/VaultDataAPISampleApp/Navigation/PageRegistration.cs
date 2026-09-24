using System.Windows.Controls;

namespace VaultDataAPISampleApp.Navigation
{
    internal sealed record PageRegistration(
        SamplePageId PageId,
        Func<Page> PageFactory);
}
