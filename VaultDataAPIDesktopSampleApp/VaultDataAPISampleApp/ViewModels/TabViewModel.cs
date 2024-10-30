using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Data;
using System.Windows;

namespace VaultDataAPISampleApp.ViewModels
{
    public class TabViewModel : INotifyPropertyChanged
    {
        private int _selectedTab;

        public int SelectedTab
        {
            get { return _selectedTab; }
            set
            {
                if (_selectedTab != value)
                {
                    _selectedTab = value;
                    OnPropertyChanged("SelectedTab");
                }
            }
        }

        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }

    //public class TabToVisibilityConverter : IValueConverter
    //{
    //    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        int tabIndex;
    //        if (int.TryParse(parameter as string, out tabIndex))
    //        {
    //            return ((int)value == tabIndex) ? Visibility.Visible : Visibility.Collapsed;
    //        }
    //        return Visibility.Collapsed;
    //    }

    //    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    //    {
    //        throw new NotImplementedException();
    //    }
    //}
}
