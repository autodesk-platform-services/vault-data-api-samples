using System;
using System.Collections.Frozen;
using System.Collections.Generic;
using System.Windows.Controls;

namespace VaultDataAPISampleApp.Navigation
{
    internal sealed class PageRegistry : IPageRegistry
    {
        private readonly FrozenDictionary<SamplePageId, Func<Page>> _pageFactories;

        public PageRegistry(IEnumerable<PageRegistration> registrations)
        {
            _pageFactories = registrations.ToFrozenDictionary(
                registration => registration.PageId,
                registration => registration.PageFactory);
        }

        public Page GetPage(SamplePageId pageId)
        {
            if (!_pageFactories.TryGetValue(pageId, out Func<Page>? pageFactory))
            {
                throw new ArgumentOutOfRangeException(
                    nameof(pageId),
                    pageId,
                    "No page is registered for this navigation item.");
            }

            return pageFactory();
        }
    }
}
