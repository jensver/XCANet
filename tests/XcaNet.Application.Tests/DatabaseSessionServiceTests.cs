using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;

namespace XcaNet.Application.Tests;

public sealed class DatabaseSessionServiceTests
{
    [Fact]
    public async Task CreateOpenUnlockLockFlow_ShouldSucceed()
    {
        var serviceProvider = BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        var createResult = await service.CreateDatabaseAsync(
            new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Test Database"),
            CancellationToken.None);

        Assert.True(createResult.IsSuccess);
        Assert.Equal(DatabaseSessionState.Unlocked, createResult.Value!.State);

        var lockResult = await service.LockDatabaseAsync(CancellationToken.None);
        Assert.True(lockResult.IsSuccess);
        Assert.Equal(DatabaseSessionState.Locked, lockResult.Value!.State);

        var unlockResult = await service.UnlockDatabaseAsync(
            new UnlockDatabaseRequest("correct horse battery staple"),
            CancellationToken.None);

        Assert.True(unlockResult.IsSuccess);
        Assert.Equal(DatabaseSessionState.Unlocked, unlockResult.Value!.State);

        var openServiceProvider = BuildServiceProvider();
        var openService = openServiceProvider.GetRequiredService<IDatabaseSessionService>();
        var openResult = await openService.OpenDatabaseAsync(new OpenDatabaseRequest(databasePath), CancellationToken.None);

        Assert.True(openResult.IsSuccess);
        Assert.Equal(DatabaseSessionState.Locked, openResult.Value!.State);
    }

    [Fact]
    public async Task Unlock_WithWrongPassword_ShouldFail()
    {
        var serviceProvider = BuildServiceProvider();
        var service = serviceProvider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(
            new CreateDatabaseRequest(databasePath, "correct horse battery staple", "Test Database"),
            CancellationToken.None);

        await service.LockDatabaseAsync(CancellationToken.None);
        var unlockResult = await service.UnlockDatabaseAsync(new UnlockDatabaseRequest("wrong password"), CancellationToken.None);

        Assert.False(unlockResult.IsSuccess);
        Assert.Equal(OperationErrorCode.InvalidPassword, unlockResult.ErrorCode);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var configuration = new ConfigurationBuilder().Build();
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddApplication(configuration);
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath()
    {
        return Path.Combine(Path.GetTempPath(), $"xcanet-app-{Guid.NewGuid():N}.db");
    }
}
