using System.Windows.Controls;

namespace VaultDataAPISampleApp.Navigation
{
    internal interface IPageRegistry
    {
        Page GetPage(SamplePageId pageId);
    }
}
