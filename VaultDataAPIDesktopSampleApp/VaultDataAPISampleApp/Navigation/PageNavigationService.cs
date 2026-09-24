using System;
using System.Windows.Controls;

namespace VaultDataAPISampleApp.Navigation
{
    internal sealed class PageNavigationService : IPageNavigationService
    {
        private readonly IPageRegistry _pageRegistry;

        private Frame? _frame;
        private SamplePageId? _currentPageId;

        public PageNavigationService(IPageRegistry pageRegistry)
        {
            _pageRegistry = pageRegistry;
        }

        public void Attach(Frame frame)
        {
            ArgumentNullException.ThrowIfNull(frame);
            frame.Dispatcher.VerifyAccess();

            if (_frame != null && !ReferenceEquals(_frame, frame))
            {
                throw new InvalidOperationException(
                    "The navigation service is already attached to a shell.");
            }

            _frame = frame;
        }

        public void Navigate(SamplePageId pageId)
        {
            if (_frame == null)
            {
                throw new InvalidOperationException(
                    "The navigation service must be initialized before navigation.");
            }

            _frame.Dispatcher.VerifyAccess();

            if (_currentPageId == pageId)
            {
                return;
            }

            Page page = _pageRegistry.GetPage(pageId);
            _frame.Navigate(page);
            while (_frame.CanGoBack)
            {
                _frame.RemoveBackEntry();
            }

            _currentPageId = pageId;
        }
    }
}
