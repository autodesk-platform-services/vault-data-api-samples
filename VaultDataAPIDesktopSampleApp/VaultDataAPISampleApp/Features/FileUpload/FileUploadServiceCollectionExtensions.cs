using Microsoft.Extensions.DependencyInjection;

using VaultDataAPISampleApp.Features.FileUpload.Dialogs;
using VaultDataAPISampleApp.Features.FileUpload.Services;
using VaultDataAPISampleApp.Features.FileUpload.ViewModels;
using VaultDataAPISampleApp.Features.FileUpload.Views;
using VaultDataAPISampleApp.Navigation;

namespace VaultDataAPISampleApp.Features.FileUpload
{
    internal static class FileUploadServiceCollectionExtensions
    {
        public static IServiceCollection AddFileUploadSample(
            this IServiceCollection services)
        {
            services.AddSingleton<ILocalFilePickerService, WpfLocalFilePickerService>();
            services.AddSingleton<IFileUploadService, FileUploadService>();
            services.AddSingleton<FileUploadViewModel>();
            services.AddSamplePage<FileUploadPage>(SamplePageId.FileUpload);
            return services;
        }
    }
}
