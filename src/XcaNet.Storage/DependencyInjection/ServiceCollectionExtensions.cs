using Microsoft.Extensions.DependencyInjection;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Repositories;

namespace XcaNet.Storage.DependencyInjection;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddStorageServices(this IServiceCollection services)
    {
        services.AddSingleton<IXcaNetDbContextFactory, SqliteXcaNetDbContextFactory>();
        services.AddSingleton<IDatabaseMigrator, DatabaseMigrator>();
        services.AddSingleton<IDatabaseProfileRepository, DatabaseProfileRepository>();
        services.AddSingleton<IAuditEventRepository, AuditEventRepository>();
        services.AddSingleton<ICertificateRepository, CertificateRepository>();
        services.AddSingleton<ICertificateRequestRepository, CertificateRequestRepository>();
        services.AddSingleton<IPrivateKeyRepository, PrivateKeyRepository>();
        return services;
    }
}
