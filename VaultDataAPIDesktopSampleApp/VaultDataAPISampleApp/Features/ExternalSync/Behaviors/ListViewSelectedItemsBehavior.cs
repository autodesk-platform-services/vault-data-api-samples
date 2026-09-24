using System.Collections;
using System.Windows;
using System.Windows.Controls;

namespace VaultDataAPISampleApp.Features.ExternalSync.Behaviors
{
    internal static class ListViewSelectedItemsBehavior
    {
        public static readonly DependencyProperty SelectedItemsProperty =
            DependencyProperty.RegisterAttached(
                "SelectedItems",
                typeof(IList),
                typeof(ListViewSelectedItemsBehavior),
                new PropertyMetadata(null, SelectedItemsChanged));

        public static IList? GetSelectedItems(DependencyObject dependencyObject)
        {
            return (IList?)dependencyObject.GetValue(SelectedItemsProperty);
        }

        public static void SetSelectedItems(
            DependencyObject dependencyObject,
            IList? value)
        {
            dependencyObject.SetValue(SelectedItemsProperty, value);
        }

        private static void SelectedItemsChanged(
            DependencyObject dependencyObject,
            DependencyPropertyChangedEventArgs e)
        {
            if (dependencyObject is not ListView listView)
            {
                return;
            }

            listView.SelectionChanged -= SelectionChanged;
            if (e.NewValue is IList)
            {
                listView.SelectionChanged += SelectionChanged;
            }
        }

        private static void SelectionChanged(
            object sender,
            SelectionChangedEventArgs e)
        {
            var listView = (ListView)sender;
            IList? selectedItems = GetSelectedItems(listView);
            if (selectedItems == null)
            {
                return;
            }

            selectedItems.Clear();
            foreach (object item in listView.SelectedItems)
            {
                selectedItems.Add(item);
            }
        }
    }
}
