using Avalonia;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XcaNet.App.Desktop.Startup;
using XcaNet.App;
using XcaNet.App.Composition;
using XcaNet.App.DependencyInjection;
using XcaNet.Application.DependencyInjection;
using XcaNet.Crypto.OpenSsl.DependencyInjection;

namespace XcaNet.App.Desktop;

internal static class Program
{
    [STAThread]
    public static int Main(string[] args)
    {
        try
        {
            var configuration = BuildConfiguration();
            var services = BuildServices(configuration);
            ServiceProviderAccessor.Initialize(services);
            StartupDiagnosticsWriter.Write(services, configuration);

            BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
            return 0;
        }
        catch (Exception ex)
        {
            var reportPath = StartupFailureReporter.Write(ex);
            Console.Error.WriteLine($"XcaNet failed to start. Details were written to: {reportPath}");
            Console.Error.WriteLine(ex.Message);
            return 1;
        }
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
            logging.SetMinimumLevel(
#if DEBUG
                LogLevel.Debug
#else
                LogLevel.Information
#endif
            );
            logging.AddSimpleConsole(options => options.SingleLine = true);
#if DEBUG
            logging.AddDebug();
#endif
        });

        services.AddXcaNetCryptoServices(configuration);
        services.AddApplication(configuration);
        services.AddPresentation();

        return services.BuildServiceProvider(validateScopes: true);
    }

    private static AppBuilder BuildAvaloniaApp()
    {
        var builder = AppBuilder.Configure<App>()
            .UsePlatformDetect();
#if DEBUG
        builder = builder.LogToTrace();
#endif
        return builder;
    }
}
