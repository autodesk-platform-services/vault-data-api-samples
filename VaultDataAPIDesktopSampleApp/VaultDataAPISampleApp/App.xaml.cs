using System.Runtime.Versioning;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using VaultDataAPISampleApp.Features.Authentication;
using VaultDataAPISampleApp.Features.ExternalSync;
using VaultDataAPISampleApp.Features.Files;
using VaultDataAPISampleApp.Features.Users;
using VaultDataAPISampleApp.Hosting;
using VaultDataAPISampleApp.Navigation;
using VaultDataAPISampleApp.ViewModels;

namespace VaultDataAPISampleApp
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    [SupportedOSPlatform("windows10.0.19041.0")]
    public partial class App : Application
    {
        private readonly IHost _host;

        public App()
        {
            var builder = Host.CreateApplicationBuilder();
            builder.Services
                .AddOxygenIdentity(builder.Configuration)
                .AddVaultDataApi(builder.Configuration)
                .AddWpfPlatform()
                .AddNavigation()
                .AddAuthenticationFeature()
                .AddFilesSample()
                .AddUsersSample()
                .AddExternalSyncSample()
                .AddShell();

            _host = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            base.OnStartup(e);
            await _host.StartAsync();

            MainWindow mainWindow =
                _host.Services.GetRequiredService<MainWindow>();
            IPageNavigationService navigationService =
                _host.Services.GetRequiredService<IPageNavigationService>();
            ShellViewModel shellViewModel =
                _host.Services.GetRequiredService<ShellViewModel>();

            navigationService.Attach(mainWindow.NavigationFrame);
            shellViewModel.Initialize();

            MainWindow = mainWindow;
            mainWindow.Show();
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }
    }
}
