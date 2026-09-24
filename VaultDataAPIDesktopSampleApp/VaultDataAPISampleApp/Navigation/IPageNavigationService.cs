using System.Windows.Controls;

namespace VaultDataAPISampleApp.Navigation
{
    internal interface IPageNavigationService
    {
        void Attach(Frame frame);

        void Navigate(SamplePageId pageId);
    }
}
