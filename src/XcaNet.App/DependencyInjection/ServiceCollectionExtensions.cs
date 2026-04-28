using Microsoft.Extensions.DependencyInjection;
using XcaNet.App.Services;
using XcaNet.App.ViewModels;
using XcaNet.App.Views;

namespace XcaNet.App.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddPresentation(this IServiceCollection services)
    {
        services.AddSingleton<IDesktopFileDialogService, DesktopFileDialogService>();
        services.AddSingleton<IUserPreferencesService, UserPreferencesService>();
        services.AddSingleton<MainWindow>();
        services.AddSingleton<ShellViewModel>();
        return services;
    }
}
