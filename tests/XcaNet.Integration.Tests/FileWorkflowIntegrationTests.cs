using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using XcaNet.App.Services;
using XcaNet.App.ViewModels;
using XcaNet.Application.DependencyInjection;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Crypto.DotNet.DependencyInjection;

namespace XcaNet.Integration.Tests;

public sealed class FileWorkflowIntegrationTests
{
    [Fact]
    public async Task ExportStoredMaterialToFileAsync_ShouldWriteCertificateArtifact()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M8 Export"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var outputPath = Path.Combine(Path.GetTempPath(), $"xcanet-export-{Guid.NewGuid():N}.pem");

        var result = await service.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(CryptoImportKind.Certificate, certificate.Value!.CertificateId, CryptoDataFormat.Pem, outputPath, null, "issuer-ca"),
            CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);
        Assert.True(File.Exists(outputPath));
        Assert.Contains("BEGIN CERTIFICATE", await File.ReadAllTextAsync(outputPath));
    }

    [Fact]
    public async Task ImportStoredFilesAsync_ShouldImportExportedCertificateFile()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M8 Import"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var outputPath = Path.Combine(Path.GetTempPath(), $"xcanet-import-{Guid.NewGuid():N}.pem");

        await service.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(CryptoImportKind.Certificate, certificate.Value!.CertificateId, CryptoDataFormat.Pem, outputPath, null, "issuer-ca"),
            CancellationToken.None);

        var importResult = await service.ImportStoredFilesAsync(new ImportStoredFilesRequest([outputPath], null), CancellationToken.None);

        Assert.True(importResult.IsSuccess, importResult.Message);
        Assert.Single(importResult.Value!.ImportedFiles);
        Assert.NotEmpty(importResult.Value.ImportedFiles[0].CertificateIds);
    }

    [Fact]
    public async Task ShellViewModel_DropImportAndDiagnostics_ShouldStayFunctional()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M8 Shell"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var outputPath = Path.Combine(Path.GetTempPath(), $"xcanet-drop-{Guid.NewGuid():N}.pem");
        await service.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(CryptoImportKind.Certificate, certificate.Value!.CertificateId, CryptoDataFormat.Pem, outputPath, null, "issuer-ca"),
            CancellationToken.None);

        var shell = new ShellViewModel(service, new FileDialogServiceStub(), NullLogger<ShellViewModel>.Instance);
        await shell.ImportFilesFromDropAsync([outputPath]);

        Assert.Equal("Available", shell.SettingsSecurityPage.ManagedBackendStatus);
        Assert.Contains("Managed", shell.SettingsSecurityPage.RoutingSummary, StringComparison.OrdinalIgnoreCase);
        Assert.NotEmpty(shell.CertificatesPage.Items);
    }

    [Fact]
    public async Task ImportStoredFilesAsync_ShouldRejectUnsupportedExtension()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M8 Invalid"), CancellationToken.None);

        var invalidPath = Path.Combine(Path.GetTempPath(), $"xcanet-invalid-{Guid.NewGuid():N}.txt");
        await File.WriteAllTextAsync(invalidPath, "not certificate material");

        var result = await service.ImportStoredFilesAsync(new ImportStoredFilesRequest([invalidPath], null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("Unsupported file type", result.Message);
    }

    [Fact]
    public async Task ImportStoredFilesAsync_ShouldRejectEmptyFilesClearly()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M12 Empty Import"), CancellationToken.None);

        var emptyPath = Path.Combine(Path.GetTempPath(), $"xcanet-empty-{Guid.NewGuid():N}.cer");
        await File.WriteAllBytesAsync(emptyPath, []);

        var result = await service.ImportStoredFilesAsync(new ImportStoredFilesRequest([emptyPath], null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("empty", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ImportStoredFilesAsync_ShouldRejectMalformedDerPayloadClearly()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M12 Bad DER"), CancellationToken.None);

        var invalidPath = Path.Combine(Path.GetTempPath(), $"xcanet-malformed-{Guid.NewGuid():N}.cer");
        await File.WriteAllBytesAsync(invalidPath, [0x01, 0x02, 0x03, 0x04]);

        var result = await service.ImportStoredFilesAsync(new ImportStoredFilesRequest([invalidPath], null), CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Contains("could not be recognized", result.Message, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task ExportStoredMaterialToFileAsync_ShouldFailGracefullyWhenDestinationIsInvalid()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M12 Export Failure"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Issuer Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certificate = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "Issuer CA", "CN=Issuer CA", 365), CancellationToken.None);
        var directoryPath = Path.Combine(Path.GetTempPath(), $"xcanet-export-dir-{Guid.NewGuid():N}");
        Directory.CreateDirectory(directoryPath);

        var result = await service.ExportStoredMaterialToFileAsync(
            new ExportStoredMaterialToFileRequest(CryptoImportKind.Certificate, certificate.Value!.CertificateId, CryptoDataFormat.Pem, directoryPath, null, "issuer-ca"),
            CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(XcaNet.Contracts.Results.OperationErrorCode.StorageFailure, result.ErrorCode);
        Assert.Contains("Could not export", result.Message);
    }

    [Fact]
    public async Task ImportPemTextAsync_ShouldImportSingleCertificateFromPemString()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M15.9 PEM Text"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Test Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certResult = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "Test CA", "CN=Test CA", 365), CancellationToken.None);
        var exported = await service.ExportStoredMaterialAsync(
            new XcaNet.Contracts.Crypto.Workflow.ExportStoredMaterialRequest(CryptoImportKind.Certificate, certResult.Value!.CertificateId, CryptoDataFormat.Pem, null, "test-ca"),
            CancellationToken.None);

        var databasePath2 = GetDatabasePath();
        await service.CloseDatabaseAsync(CancellationToken.None);
        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath2, "correct horse battery staple", "M15.9 PEM Import"), CancellationToken.None);

        var result = await service.ImportPemTextAsync(exported.Value!.TextRepresentation!, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);
        Assert.Single(result.Value!.CertificateIds);
        Assert.Empty(result.Value.PrivateKeyIds);
    }

    [Fact]
    public async Task ImportPemTextAsync_ShouldImportMultipleObjectsFromPemString()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M15.9 Multi PEM"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Key1", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var cert1 = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "CA1", "CN=CA1", 365), CancellationToken.None);
        var key2 = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("Key2", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var cert2 = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key2.Value!.PrivateKeyId, "CA2", "CN=CA2", 365), CancellationToken.None);

        var export1 = await service.ExportStoredMaterialAsync(
            new XcaNet.Contracts.Crypto.Workflow.ExportStoredMaterialRequest(CryptoImportKind.Certificate, cert1.Value!.CertificateId, CryptoDataFormat.Pem, null, "ca1"),
            CancellationToken.None);
        var export2 = await service.ExportStoredMaterialAsync(
            new XcaNet.Contracts.Crypto.Workflow.ExportStoredMaterialRequest(CryptoImportKind.Certificate, cert2.Value!.CertificateId, CryptoDataFormat.Pem, null, "ca2"),
            CancellationToken.None);

        var combinedPem = export1.Value!.TextRepresentation! + "\n" + export2.Value!.TextRepresentation!;

        var databasePath2 = GetDatabasePath();
        await service.CloseDatabaseAsync(CancellationToken.None);
        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath2, "correct horse battery staple", "M15.9 Multi Import"), CancellationToken.None);

        var result = await service.ImportPemTextAsync(combinedPem, CancellationToken.None);

        Assert.True(result.IsSuccess, result.Message);
        Assert.Equal(2, result.Value!.CertificateIds.Count);
    }

    [Fact]
    public async Task ImportPemTextAsync_ShouldReturnFailureForEmptyOrNonPemText()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();
        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M15.9 No PEM"), CancellationToken.None);

        var result = await service.ImportPemTextAsync("this is not pem data", CancellationToken.None);

        Assert.False(result.IsSuccess);
        Assert.Equal(XcaNet.Contracts.Results.OperationErrorCode.ValidationFailed, result.ErrorCode);
    }

    [Fact]
    public async Task ImportStoredFilesAsync_ShouldClassifyP7bExtensionAsPkcs7()
    {
        using var provider = BuildServiceProvider();
        var service = provider.GetRequiredService<IDatabaseSessionService>();
        var databasePath = GetDatabasePath();

        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath, "correct horse battery staple", "M15.9 P7B"), CancellationToken.None);
        var key = await service.GenerateStoredKeyAsync(new GenerateStoredKeyRequest("P7B Key", KeyAlgorithmKind.Rsa, 3072, null), CancellationToken.None);
        var certResult = await service.CreateSelfSignedCaAsync(new CreateSelfSignedCaWorkflowRequest(key.Value!.PrivateKeyId, "P7B CA", "CN=P7B CA", 365), CancellationToken.None);

        var pemExport = await service.ExportStoredMaterialAsync(
            new XcaNet.Contracts.Crypto.Workflow.ExportStoredMaterialRequest(CryptoImportKind.Certificate, certResult.Value!.CertificateId, CryptoDataFormat.Pem, null, "p7b-ca"),
            CancellationToken.None);

        var collection = new System.Security.Cryptography.X509Certificates.X509Certificate2Collection();
        collection.Add(System.Security.Cryptography.X509Certificates.X509Certificate2.CreateFromPem(pemExport.Value!.TextRepresentation!));

#pragma warning disable SYSLIB0057
        var p7bBytes = collection.Export(System.Security.Cryptography.X509Certificates.X509ContentType.Pkcs7)!;
#pragma warning restore SYSLIB0057

        var p7bPath = Path.Combine(Path.GetTempPath(), $"xcanet-m15-9-{Guid.NewGuid():N}.p7b");
        await File.WriteAllBytesAsync(p7bPath, p7bBytes);

        var databasePath2 = GetDatabasePath();
        await service.CloseDatabaseAsync(CancellationToken.None);
        await service.CreateDatabaseAsync(new CreateDatabaseRequest(databasePath2, "correct horse battery staple", "M15.9 P7B Import"), CancellationToken.None);

        var importResult = await service.ImportStoredFilesAsync(new ImportStoredFilesRequest([p7bPath], null), CancellationToken.None);

        Assert.True(importResult.IsSuccess, importResult.Message);
        Assert.Single(importResult.Value!.ImportedFiles);
        Assert.NotEmpty(importResult.Value.ImportedFiles[0].CertificateIds);
    }

    private static ServiceProvider BuildServiceProvider()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddManagedCryptoServices();
        services.AddApplication(new ConfigurationBuilder().Build());
        return services.BuildServiceProvider();
    }

    private static string GetDatabasePath() => Path.Combine(Path.GetTempPath(), $"xcanet-int-m8-{Guid.NewGuid():N}.db");

    private sealed class FileDialogServiceStub : IDesktopFileDialogService
    {
        public void SetOwner(Avalonia.Controls.Window? window)
        {
        }

        public Task<IReadOnlyList<string>> PickImportFilesAsync(CancellationToken cancellationToken)
            => Task.FromResult<IReadOnlyList<string>>([]);

        public Task<string?> PickSavePathAsync(string suggestedFileName, CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);

        public Task<string?> GetClipboardTextAsync(CancellationToken cancellationToken)
            => Task.FromResult<string?>(null);
    }
}
