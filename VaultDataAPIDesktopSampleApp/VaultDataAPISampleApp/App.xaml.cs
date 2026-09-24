using System.Runtime.Versioning;
using System.Windows;

using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using VaultDataAPISampleApp.Services;
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
                .AddOptions<IdentityOptions>()
                .Bind(builder.Configuration.GetSection(IdentityOptions.SectionName))
                .Validate(options =>
                    IdentityOptions.IsAbsoluteHttps(options.AuthorizationEndpoint),
                    "AuthorizationEndpoint must be an absolute HTTPS URL.")
                .Validate(options =>
                    IdentityOptions.IsAbsoluteHttps(options.TokenEndpoint),
                    "TokenEndpoint must be an absolute HTTPS URL.")
                .Validate(options =>
                    IdentityOptions.IsLoopbackHttp(options.RedirectUri),
                    "RedirectUri must be a loopback HTTP URL without query or fragment.")
                .Validate(
                    options => !string.IsNullOrWhiteSpace(options.Scope),
                    "Scope is required.")
                .ValidateOnStart();

            builder.Services
                .AddOptions<VaultOptions>()
                .Bind(builder.Configuration.GetSection(VaultOptions.SectionName))
                .Validate(
                    options => VaultOptions.IsValidApiBaseUri(options.ApiBaseUri),
                    "ApiBaseUri must be a root-relative path without query or fragment.")
                .ValidateOnStart();

            builder.Services.AddHttpClient();

            builder.Services.AddSingleton<IIdentityService, IdentityService>();
            builder.Services.AddSingleton<VaultAPIService>();

            builder.Services.AddTransient<TabViewModel>();
            builder.Services.AddTransient<ExternalSyncTabControl>();
            builder.Services.AddTransient<MainWindow>();

            _host = builder.Build();
        }

        protected override async void OnStartup(StartupEventArgs e)
        {
            await _host.StartAsync();

            MainWindow mainWindow = _host.Services.GetRequiredService<MainWindow>();
            mainWindow.Show();

            base.OnStartup(e);
        }

        protected override async void OnExit(ExitEventArgs e)
        {
            await _host.StopAsync();
            _host.Dispose();

            base.OnExit(e);
        }

    }
}
