using System;

namespace XcaNet.App.Composition;

public static class ServiceProviderAccessor
{
    private static IServiceProvider? _services;

    public static IServiceProvider Services =>
        _services ?? throw new InvalidOperationException("The application service provider has not been initialized.");

    public static void Initialize(IServiceProvider services)
    {
        _services = services;
    }
}
