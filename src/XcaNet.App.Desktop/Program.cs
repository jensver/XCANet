using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XcaNet.App;
using XcaNet.App.Composition;
using XcaNet.App.DependencyInjection;
using XcaNet.Application.DependencyInjection;

namespace XcaNet.App.Desktop;

internal static class Program
{
    [STAThread]
    public static void Main(string[] args)
    {
        var configuration = BuildConfiguration();
        var services = BuildServices(configuration);
        ServiceProviderAccessor.Initialize(services);

        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    private static IConfiguration BuildConfiguration()
    {
        return new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables(prefix: "XCANET_")
            .Build();
    }

    private static IServiceProvider BuildServices(IConfiguration configuration)
    {
        var services = new ServiceCollection();

        services.AddSingleton(configuration);
        services.AddLogging(logging =>
        {
            logging.ClearProviders();
            logging.AddSimpleConsole(options => options.SingleLine = true);
            logging.AddDebug();
        });

        services.AddApplication(configuration);
        services.AddPresentation();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        return AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .LogToTrace();
    }
}
