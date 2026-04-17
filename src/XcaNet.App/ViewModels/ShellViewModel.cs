using System.ComponentModel;
using System.Runtime.CompilerServices;
using System.Text;
using XcaNet.App.Commands;
using XcaNet.Application.Services;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;
using Microsoft.Extensions.Logging;

namespace XcaNet.App.ViewModels;

public sealed class ShellViewModel : INotifyPropertyChanged
{
    private readonly IDatabaseSessionService _databaseSessionService;
    private readonly ILogger<ShellViewModel> _logger;
    private string _databasePath;
    private string _password;
    private string _displayName;
    private string _statusMessage;
    private string _subtitle;
    private string _keyDisplayName;
    private string _keyAlgorithm;
    private string _ellipticCurve;
    private string _subjectName;
    private string _currentPrivateKeyId;
    private string _issuerCertificateId;
    private string _currentCertificateId;
    private string _currentCertificateSigningRequestId;
    private string _importKind;
    private string _importFormat;
    private string _importPayload;
    private string _exportKind;
    private string _exportFormat;
    private string _exportPassword;
    private string _exportedArtifact;
    private string _certificateDetails;

    public ShellViewModel(IDatabaseSessionService databaseSessionService, ILogger<ShellViewModel> logger)
    {
        _databaseSessionService = databaseSessionService;
        _logger = logger;

        logger.LogInformation("Initializing XcaNet shell.");

        Title = "XcaNet";
        _subtitle = "Milestone 2 storage and security";
        _databasePath = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), "XcaNet", "xcanet.db");
        _displayName = "Primary XcaNet Database";
        _password = string.Empty;
        _statusMessage = _databaseSessionService.GetSnapshot().StatusMessage;
        _keyDisplayName = "Root Key";
        _keyAlgorithm = "Rsa";
        _ellipticCurve = "P256";
        _subjectName = "CN=Root CA";
        _currentPrivateKeyId = string.Empty;
        _issuerCertificateId = string.Empty;
        _currentCertificateId = string.Empty;
        _currentCertificateSigningRequestId = string.Empty;
        _importKind = "Certificate";
        _importFormat = "Pem";
        _importPayload = string.Empty;
        _exportKind = "Certificate";
        _exportFormat = "Pem";
        _exportPassword = string.Empty;
        _exportedArtifact = string.Empty;
        _certificateDetails = "No certificate details loaded.";

        CreateDatabaseCommand = new AsyncCommand(CreateDatabaseAsync);
        OpenDatabaseCommand = new AsyncCommand(OpenDatabaseAsync);
        UnlockDatabaseCommand = new AsyncCommand(UnlockDatabaseAsync);
        LockDatabaseCommand = new AsyncCommand(LockDatabaseAsync);
        GenerateKeyCommand = new AsyncCommand(GenerateKeyAsync);
        CreateSelfSignedCaCommand = new AsyncCommand(CreateSelfSignedCaAsync);
        CreateCertificateSigningRequestCommand = new AsyncCommand(CreateCertificateSigningRequestAsync);
        SignCertificateSigningRequestCommand = new AsyncCommand(SignCertificateSigningRequestAsync);
        ImportMaterialCommand = new AsyncCommand(ImportMaterialAsync);
        ExportMaterialCommand = new AsyncCommand(ExportMaterialAsync);
        LoadCertificateDetailsCommand = new AsyncCommand(LoadCertificateDetailsAsync);
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    public string Title { get; }

    public string Subtitle
    {
        get => _subtitle;
        private set => SetProperty(ref _subtitle, value);
    }

    public string DatabasePath
    {
        get => _databasePath;
        set => SetProperty(ref _databasePath, value);
    }

    public string Password
    {
        get => _password;
        set => SetProperty(ref _password, value);
    }

    public string DisplayName
    {
        get => _displayName;
        set => SetProperty(ref _displayName, value);
    }

    public string StatusMessage
    {
        get => _statusMessage;
        private set => SetProperty(ref _statusMessage, value);
    }

    public string KeyDisplayName
    {
        get => _keyDisplayName;
        set => SetProperty(ref _keyDisplayName, value);
    }

    public string KeyAlgorithm
    {
        get => _keyAlgorithm;
        set => SetProperty(ref _keyAlgorithm, value);
    }

    public string EllipticCurve
    {
        get => _ellipticCurve;
        set => SetProperty(ref _ellipticCurve, value);
    }

    public string SubjectName
    {
        get => _subjectName;
        set => SetProperty(ref _subjectName, value);
    }

    public string CurrentPrivateKeyId
    {
        get => _currentPrivateKeyId;
        set => SetProperty(ref _currentPrivateKeyId, value);
    }

    public string IssuerCertificateId
    {
        get => _issuerCertificateId;
        set => SetProperty(ref _issuerCertificateId, value);
    }

    public string CurrentCertificateId
    {
        get => _currentCertificateId;
        set => SetProperty(ref _currentCertificateId, value);
    }

    public string CurrentCertificateSigningRequestId
    {
        get => _currentCertificateSigningRequestId;
        set => SetProperty(ref _currentCertificateSigningRequestId, value);
    }

    public string ImportKind
    {
        get => _importKind;
        set => SetProperty(ref _importKind, value);
    }

    public string ImportFormat
    {
        get => _importFormat;
        set => SetProperty(ref _importFormat, value);
    }

    public string ImportPayload
    {
        get => _importPayload;
        set => SetProperty(ref _importPayload, value);
    }

    public string ExportKind
    {
        get => _exportKind;
        set => SetProperty(ref _exportKind, value);
    }

    public string ExportFormat
    {
        get => _exportFormat;
        set => SetProperty(ref _exportFormat, value);
    }

    public string ExportPassword
    {
        get => _exportPassword;
        set => SetProperty(ref _exportPassword, value);
    }

    public string ExportedArtifact
    {
        get => _exportedArtifact;
        private set => SetProperty(ref _exportedArtifact, value);
    }

    public string CertificateDetails
    {
        get => _certificateDetails;
        private set => SetProperty(ref _certificateDetails, value);
    }

    public DatabaseSessionSnapshot Snapshot => _databaseSessionService.GetSnapshot();

    public AsyncCommand CreateDatabaseCommand { get; }

    public AsyncCommand OpenDatabaseCommand { get; }

    public AsyncCommand UnlockDatabaseCommand { get; }

    public AsyncCommand LockDatabaseCommand { get; }

    public AsyncCommand GenerateKeyCommand { get; }

    public AsyncCommand CreateSelfSignedCaCommand { get; }

    public AsyncCommand CreateCertificateSigningRequestCommand { get; }

    public AsyncCommand SignCertificateSigningRequestCommand { get; }

    public AsyncCommand ImportMaterialCommand { get; }

    public AsyncCommand ExportMaterialCommand { get; }

    public AsyncCommand LoadCertificateDetailsCommand { get; }

    private async Task CreateDatabaseAsync()
    {
        var result = await _databaseSessionService.CreateDatabaseAsync(
            new CreateDatabaseRequest(DatabasePath, Password, DisplayName),
            CancellationToken.None);

        ApplyResult(result);
    }

    private async Task OpenDatabaseAsync()
    {
        var result = await _databaseSessionService.OpenDatabaseAsync(
            new OpenDatabaseRequest(DatabasePath),
            CancellationToken.None);

        ApplyResult(result);
    }

    private async Task UnlockDatabaseAsync()
    {
        var result = await _databaseSessionService.UnlockDatabaseAsync(
            new UnlockDatabaseRequest(Password),
            CancellationToken.None);

        ApplyResult(result);
    }

    private async Task LockDatabaseAsync()
    {
        var result = await _databaseSessionService.LockDatabaseAsync(CancellationToken.None);
        ApplyResult(result);
    }

    private async Task GenerateKeyAsync()
    {
        if (!Enum.TryParse<KeyAlgorithmKind>(KeyAlgorithm, ignoreCase: true, out var algorithm))
        {
            StatusMessage = "Invalid key algorithm. Use Rsa or Ecdsa.";
            return;
        }

        EllipticCurveKind? curve = null;
        if (algorithm == KeyAlgorithmKind.Ecdsa && Enum.TryParse<EllipticCurveKind>(EllipticCurve, ignoreCase: true, out var parsedCurve))
        {
            curve = parsedCurve;
        }

        var result = await _databaseSessionService.GenerateStoredKeyAsync(
            new GenerateStoredKeyRequest(KeyDisplayName, algorithm, algorithm == KeyAlgorithmKind.Rsa ? 3072 : null, curve),
            CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            CurrentPrivateKeyId = result.Value.PrivateKeyId.ToString();
            StatusMessage = $"Stored {result.Value.Algorithm} key {CurrentPrivateKeyId}.";
        }
        else
        {
            StatusMessage = result.Message;
        }
    }

    private async Task CreateSelfSignedCaAsync()
    {
        if (!Guid.TryParse(CurrentPrivateKeyId, out var privateKeyId))
        {
            StatusMessage = "Enter a valid private key id.";
            return;
        }

        var result = await _databaseSessionService.CreateSelfSignedCaAsync(
            new CreateSelfSignedCaWorkflowRequest(privateKeyId, $"{KeyDisplayName} CA", SubjectName, 3650),
            CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            CurrentCertificateId = result.Value.CertificateId.ToString();
            IssuerCertificateId = CurrentCertificateId;
            CertificateDetails = FormatCertificateDetails(result.Value.Details);
        }

        StatusMessage = result.Message;
    }

    private async Task CreateCertificateSigningRequestAsync()
    {
        if (!Guid.TryParse(CurrentPrivateKeyId, out var privateKeyId))
        {
            StatusMessage = "Enter a valid private key id.";
            return;
        }

        var result = await _databaseSessionService.CreateCertificateSigningRequestAsync(
            new CreateCertificateSigningRequestWorkflowRequest(privateKeyId, $"{KeyDisplayName} CSR", SubjectName, []),
            CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            CurrentCertificateSigningRequestId = result.Value.CertificateSigningRequestId.ToString();
            StatusMessage = $"Created CSR {CurrentCertificateSigningRequestId}.";
        }
        else
        {
            StatusMessage = result.Message;
        }
    }

    private async Task SignCertificateSigningRequestAsync()
    {
        if (!Guid.TryParse(CurrentCertificateSigningRequestId, out var csrId) ||
            !Guid.TryParse(IssuerCertificateId, out var issuerCertificateId) ||
            !Guid.TryParse(CurrentPrivateKeyId, out var issuerPrivateKeyId))
        {
            StatusMessage = "Enter valid CSR, issuer certificate, and issuer key ids.";
            return;
        }

        var result = await _databaseSessionService.SignCertificateSigningRequestAsync(
            new SignStoredCertificateSigningRequestRequest(csrId, issuerCertificateId, issuerPrivateKeyId, "Issued Certificate", 365),
            CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            CurrentCertificateId = result.Value.CertificateId.ToString();
            CertificateDetails = FormatCertificateDetails(result.Value.Details);
        }

        StatusMessage = result.Message;
    }

    private async Task ImportMaterialAsync()
    {
        if (!Enum.TryParse<CryptoImportKind>(ImportKind, ignoreCase: true, out var kind) ||
            !Enum.TryParse<CryptoDataFormat>(ImportFormat, ignoreCase: true, out var format))
        {
            StatusMessage = "Invalid import kind or format.";
            return;
        }

        var result = await _databaseSessionService.ImportStoredMaterialAsync(
            new ImportStoredMaterialRequest(KeyDisplayName, kind, format, Encoding.UTF8.GetBytes(ImportPayload), null),
            CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            if (result.Value.PrivateKeyIds.Count > 0)
            {
                CurrentPrivateKeyId = result.Value.PrivateKeyIds[0].ToString();
            }

            if (result.Value.CertificateIds.Count > 0)
            {
                CurrentCertificateId = result.Value.CertificateIds[0].ToString();
                IssuerCertificateId = CurrentCertificateId;
            }

            if (result.Value.CertificateSigningRequestIds.Count > 0)
            {
                CurrentCertificateSigningRequestId = result.Value.CertificateSigningRequestIds[0].ToString();
            }
        }

        StatusMessage = result.Message;
    }

    private async Task ExportMaterialAsync()
    {
        if (!Enum.TryParse<CryptoImportKind>(ExportKind, ignoreCase: true, out var kind) ||
            !Enum.TryParse<CryptoDataFormat>(ExportFormat, ignoreCase: true, out var format))
        {
            StatusMessage = "Invalid export kind or format.";
            return;
        }

        var materialId = kind switch
        {
            CryptoImportKind.PrivateKey => CurrentPrivateKeyId,
            CryptoImportKind.Certificate => CurrentCertificateId,
            CryptoImportKind.CertificateSigningRequest => CurrentCertificateSigningRequestId,
            _ => string.Empty
        };

        if (!Guid.TryParse(materialId, out var parsedMaterialId))
        {
            StatusMessage = "Enter a valid material id to export.";
            return;
        }

        var result = await _databaseSessionService.ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(kind, parsedMaterialId, format, ExportPassword, "xcanet-export"),
            CancellationToken.None);

        if (result.IsSuccess && result.Value is not null)
        {
            ExportedArtifact = result.Value.TextRepresentation ?? Convert.ToBase64String(result.Value.Data);
        }

        StatusMessage = result.Message;
    }

    private async Task LoadCertificateDetailsAsync()
    {
        if (!Guid.TryParse(CurrentCertificateId, out var certificateId))
        {
            StatusMessage = "Enter a valid certificate id.";
            return;
        }

        var result = await _databaseSessionService.GetCertificateDetailsAsync(certificateId, CancellationToken.None);
        if (result.IsSuccess && result.Value is not null)
        {
            CertificateDetails = FormatCertificateDetails(result.Value);
        }

        StatusMessage = result.Message;
    }

    private void ApplyResult(OperationResult<DatabaseSessionSnapshot> result)
    {
        Subtitle = Snapshot.State switch
        {
            DatabaseSessionState.Unlocked => "Database unlocked",
            DatabaseSessionState.Locked => "Database open",
            _ => "Milestone 2 storage and security"
        };

        StatusMessage = result.Message;
        _logger.LogInformation("Database UI action completed with state {State}.", Snapshot.State);

        OnPropertyChanged(nameof(Snapshot));
    }

    private static string FormatCertificateDetails(CertificateDetails details)
    {
        return string.Join(
            Environment.NewLine,
            [
                $"Subject: {details.Subject}",
                $"Issuer: {details.Issuer}",
                $"Serial: {details.SerialNumber}",
                $"Validity: {details.NotBefore:u} -> {details.NotAfter:u}",
                $"SHA-1: {details.Sha1Thumbprint}",
                $"SHA-256: {details.Sha256Thumbprint}",
                $"Algorithm: {details.KeyAlgorithm}",
                $"CA: {details.IsCertificateAuthority}",
                $"Key Usage: {string.Join(", ", details.KeyUsages)}",
                $"EKU: {string.Join(", ", details.EnhancedKeyUsages)}",
                $"SAN: {string.Join(", ", details.SubjectAlternativeNames)}"
            ]);
    }

    private void SetProperty<T>(ref T field, T value, [CallerMemberName] string? propertyName = null)
    {
        if (EqualityComparer<T>.Default.Equals(field, value))
        {
            return;
        }

        field = value;
        OnPropertyChanged(propertyName);
    }

    private void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
