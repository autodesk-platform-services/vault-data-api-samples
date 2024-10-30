using System.Configuration;
using System.Data;
using System.Windows;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application
    {
        private void ApplicationStart(object sender, StartupEventArgs e)
        {
            //Disable shutdown when the dialog closes
            Current.ShutdownMode = ShutdownMode.OnExplicitShutdown;

            var mainWindow = new MainWindow();
            Current.MainWindow = mainWindow;
            mainWindow.Show();
        }

    }
}
