using Microsoft.Extensions.Logging;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;
using XcaNet.Crypto.Abstractions;
using XcaNet.Core.Enums;
using XcaNet.Security.Protection;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;
using XcaNet.Storage.Repositories;

namespace XcaNet.Application.Services;

public sealed class DatabaseSessionService : IDatabaseSessionService, IDisposable
{
    private readonly IDatabaseMigrator _databaseMigrator;
    private readonly IDatabaseProfileRepository _databaseProfileRepository;
    private readonly IAuditEventRepository _auditEventRepository;
    private readonly ICertificateRepository _certificateRepository;
    private readonly ICertificateRequestRepository _certificateRequestRepository;
    private readonly ICertificateRevocationListRepository _certificateRevocationListRepository;
    private readonly IPrivateKeyRepository _privateKeyRepository;
    private readonly ITemplateRepository _templateRepository;
    private readonly IDatabaseSecretProtector _databaseSecretProtector;
    private readonly IKeyService _keyService;
    private readonly ICertificateService _certificateService;
    private readonly ICertificateSigningRequestService _certificateSigningRequestService;
    private readonly IImportExportService _importExportService;
    private readonly ILogger<DatabaseSessionService> _logger;

    private readonly SemaphoreSlim _gate = new(1, 1);
    private string? _currentDatabasePath;
    private string? _currentDisplayName;
    private DateTime? _lastOpenedUtc;
    private UnlockedDatabaseKey? _unlockedKey;
    private DatabaseSessionState _state = DatabaseSessionState.Closed;

    public DatabaseSessionService(
        IDatabaseMigrator databaseMigrator,
        IDatabaseProfileRepository databaseProfileRepository,
        IAuditEventRepository auditEventRepository,
        ICertificateRepository certificateRepository,
        ICertificateRequestRepository certificateRequestRepository,
        ICertificateRevocationListRepository certificateRevocationListRepository,
        IPrivateKeyRepository privateKeyRepository,
        ITemplateRepository templateRepository,
        IDatabaseSecretProtector databaseSecretProtector,
        IKeyService keyService,
        ICertificateService certificateService,
        ICertificateSigningRequestService certificateSigningRequestService,
        IImportExportService importExportService,
        ILogger<DatabaseSessionService> logger)
    {
        _databaseMigrator = databaseMigrator;
        _databaseProfileRepository = databaseProfileRepository;
        _auditEventRepository = auditEventRepository;
        _certificateRepository = certificateRepository;
        _certificateRequestRepository = certificateRequestRepository;
        _certificateRevocationListRepository = certificateRevocationListRepository;
        _privateKeyRepository = privateKeyRepository;
        _templateRepository = templateRepository;
        _databaseSecretProtector = databaseSecretProtector;
        _keyService = keyService;
        _certificateService = certificateService;
        _certificateSigningRequestService = certificateSigningRequestService;
        _importExportService = importExportService;
        _logger = logger;
    }

    public async Task<OperationResult<DatabaseSessionSnapshot>> CreateDatabaseAsync(CreateDatabaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatabasePath) || string.IsNullOrWhiteSpace(request.DisplayName))
        {
            return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.ValidationFailed, "Database path and display name are required.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(request.DatabasePath) ?? ".");
            await _databaseMigrator.MigrateAsync(request.DatabasePath, cancellationToken);

            if (await _databaseProfileRepository.ExistsAsync(request.DatabasePath, cancellationToken))
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.DatabaseAlreadyExists, "A database profile already exists at the selected path.");
            }

            var protectionResult = _databaseSecretProtector.CreateProfile(request.Password);
            if (!protectionResult.IsSuccess)
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(protectionResult.ErrorCode, protectionResult.Message);
            }

            var (profile, unlockedKey) = protectionResult.Value!;
            ReplaceUnlockedKey(unlockedKey);

            var databaseProfile = new DatabaseProfileEntity
            {
                Id = Guid.NewGuid(),
                DisplayName = request.DisplayName,
                KdfAlgorithm = profile.KdfAlgorithm,
                KdfIterations = profile.KdfIterations,
                KdfSalt = profile.KdfSalt,
                VerifierNonce = profile.VerifierNonce,
                VerifierCiphertext = profile.VerifierCiphertext,
                VerifierTag = profile.VerifierTag,
                EncryptionAlgorithm = profile.EncryptionAlgorithm,
                KeyVersion = profile.KeyVersion,
                SchemaVersion = XcaNetDbContext.CurrentSchemaVersion,
                CreatedUtc = DateTime.UtcNow,
                LastOpenedUtc = DateTime.UtcNow
            };

            await _databaseProfileRepository.AddAsync(request.DatabasePath, databaseProfile, cancellationToken);
            await _auditEventRepository.AddAsync(request.DatabasePath, CreateAuditEvent(AuditEventKind.DatabaseCreated, "Database created."), cancellationToken);
            await _auditEventRepository.AddAsync(request.DatabasePath, CreateAuditEvent(AuditEventKind.DatabaseUnlocked, "Database unlocked."), cancellationToken);

            _currentDatabasePath = request.DatabasePath;
            _currentDisplayName = request.DisplayName;
            _lastOpenedUtc = databaseProfile.LastOpenedUtc;
            _state = DatabaseSessionState.Unlocked;

            _logger.LogInformation("Database initialized at {DatabasePath}.", request.DatabasePath);
            return OperationResult<DatabaseSessionSnapshot>.Success(GetSnapshot(), "Database created and unlocked.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<DatabaseSessionSnapshot>> OpenDatabaseAsync(OpenDatabaseRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DatabasePath) || !File.Exists(request.DatabasePath))
        {
            return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.DatabaseNotFound, "The selected database file was not found.");
        }

        await _gate.WaitAsync(cancellationToken);
        try
        {
            await _databaseMigrator.MigrateAsync(request.DatabasePath, cancellationToken);
            var profile = await _databaseProfileRepository.GetAsync(request.DatabasePath, cancellationToken);
            if (profile is null)
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.DatabaseNotFound, "The selected database does not contain an XcaNet profile.");
            }

            ClearUnlockedKey();
            _currentDatabasePath = request.DatabasePath;
            _currentDisplayName = profile.DisplayName;
            _lastOpenedUtc = DateTime.UtcNow;
            _state = DatabaseSessionState.Locked;

            await _databaseProfileRepository.UpdateLastOpenedUtcAsync(request.DatabasePath, profile.Id, _lastOpenedUtc.Value, cancellationToken);
            await _auditEventRepository.AddAsync(request.DatabasePath, CreateAuditEvent(AuditEventKind.DatabaseOpened, "Database opened."), cancellationToken);

            return OperationResult<DatabaseSessionSnapshot>.Success(GetSnapshot(), "Database opened in locked mode.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<DatabaseSessionSnapshot>> UnlockDatabaseAsync(UnlockDatabaseRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(_currentDatabasePath))
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.DatabaseNotOpen, "Open a database before unlocking it.");
            }

            var profileEntity = await _databaseProfileRepository.GetAsync(_currentDatabasePath, cancellationToken);
            if (profileEntity is null)
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.DatabaseNotFound, "The current database profile could not be loaded.");
            }

            var unlockResult = _databaseSecretProtector.Unlock(request.Password, MapProfile(profileEntity));
            if (!unlockResult.IsSuccess)
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(unlockResult.ErrorCode, unlockResult.Message);
            }

            ReplaceUnlockedKey(unlockResult.Value!);
            _state = DatabaseSessionState.Unlocked;
            await _auditEventRepository.AddAsync(_currentDatabasePath, CreateAuditEvent(AuditEventKind.DatabaseUnlocked, "Database unlocked."), cancellationToken);
            return OperationResult<DatabaseSessionSnapshot>.Success(GetSnapshot(), "Database unlocked.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<DatabaseSessionSnapshot>> LockDatabaseAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(_currentDatabasePath))
            {
                return OperationResult<DatabaseSessionSnapshot>.Failure(OperationErrorCode.DatabaseNotOpen, "No database is currently open.");
            }

            ClearUnlockedKey();
            _state = DatabaseSessionState.Locked;
            await _auditEventRepository.AddAsync(_currentDatabasePath, CreateAuditEvent(AuditEventKind.DatabaseLocked, "Database locked."), cancellationToken);
            return OperationResult<DatabaseSessionSnapshot>.Success(GetSnapshot(), "Database locked.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<StorePrivateKeyResult>> StorePrivateKeyAsync(StorePrivateKeyRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(_currentDatabasePath))
            {
                return OperationResult<StorePrivateKeyResult>.Failure(OperationErrorCode.DatabaseNotOpen, "No database is currently open.");
            }

            if (_state != DatabaseSessionState.Unlocked || _unlockedKey is null)
            {
                return OperationResult<StorePrivateKeyResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before storing private keys.");
            }

            var encryptionResult = _databaseSecretProtector.EncryptPrivateKey(request.Pkcs8Bytes, _unlockedKey);
            if (!encryptionResult.IsSuccess)
            {
                return OperationResult<StorePrivateKeyResult>.Failure(encryptionResult.ErrorCode, encryptionResult.Message);
            }

            var encryptedPayload = encryptionResult.Value!;
            var privateKeyId = Guid.NewGuid();
            var entity = new PrivateKeyEntity
            {
                Id = privateKeyId,
                DisplayName = request.DisplayName,
                Algorithm = request.Algorithm,
                PublicKeyFingerprint = request.PublicKeyFingerprint,
                EncryptedPkcs8Ciphertext = encryptedPayload.Ciphertext,
                EncryptionNonce = encryptedPayload.Nonce,
                EncryptionTag = encryptedPayload.Tag,
                EncryptionAlgorithm = encryptedPayload.EncryptionAlgorithm,
                KeyVersion = encryptedPayload.KeyVersion,
                CreatedUtc = DateTime.UtcNow
            };

            await _privateKeyRepository.AddAsync(_currentDatabasePath, entity, cancellationToken);
            await _auditEventRepository.AddAsync(
                _currentDatabasePath,
                CreateAuditEvent(GetPrivateKeyAuditEvent(request.Source), "Private key stored."),
                cancellationToken);

            return OperationResult<StorePrivateKeyResult>.Success(new StorePrivateKeyResult(privateKeyId), "Private key stored securely.");
        }
        finally
        {
            Array.Clear(request.Pkcs8Bytes, 0, request.Pkcs8Bytes.Length);
            _gate.Release();
        }
    }

    public async Task<OperationResult<StoredKeyResult>> GenerateStoredKeyAsync(GenerateStoredKeyRequest request, CancellationToken cancellationToken)
    {
        var generateResult = await _keyService.GenerateAsync(
            new GenerateKeyPairRequest(request.DisplayName, request.Algorithm, request.RsaKeySize, request.Curve),
            cancellationToken);

        if (!generateResult.IsSuccess || generateResult.Value is null)
        {
            return OperationResult<StoredKeyResult>.Failure(generateResult.ErrorCode, generateResult.Message);
        }

        var storeResult = await StorePrivateKeyAsync(
            new StorePrivateKeyRequest(
                request.DisplayName,
                generateResult.Value.Algorithm,
                generateResult.Value.PublicKeyFingerprint,
                generateResult.Value.Pkcs8PrivateKey,
                "generated"),
            cancellationToken);

        return !storeResult.IsSuccess || storeResult.Value is null
            ? OperationResult<StoredKeyResult>.Failure(storeResult.ErrorCode, storeResult.Message)
            : OperationResult<StoredKeyResult>.Success(
                new StoredKeyResult(storeResult.Value.PrivateKeyId, generateResult.Value.Algorithm, generateResult.Value.PublicKeyFingerprint),
                "Private key generated and stored.");
    }

    public async Task<OperationResult<StoredCertificateResult>> CreateSelfSignedCaAsync(CreateSelfSignedCaWorkflowRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var privateKeyEntity = await EnsureUnlockedPrivateKeyAsync(request.PrivateKeyId, cancellationToken);
            if (privateKeyEntity is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before certificate operations.");
            }

            var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
            var createResult = await _certificateService.CreateSelfSignedCaAsync(
                new SelfSignedCaCertificateRequest(request.SubjectName, decryptedPrivateKey, privateKeyEntity.Algorithm, request.ValidityDays),
                cancellationToken);

            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);

            if (!createResult.IsSuccess || createResult.Value is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(createResult.ErrorCode, createResult.Message);
            }

            var certificateId = Guid.NewGuid();
            await _certificateRepository.AddAsync(_currentDatabasePath!, CreateCertificateEntity(certificateId, request.DisplayName, createResult.Value.DerData, createResult.Value.Details, request.PrivateKeyId, null), cancellationToken);
            await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.CertificateCreated, "Certificate created."), cancellationToken);

            return OperationResult<StoredCertificateResult>.Success(
                new StoredCertificateResult(certificateId, request.PrivateKeyId, createResult.Value.Details),
                "Self-signed CA certificate created.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<StoredCertificateSigningRequestResult>> CreateCertificateSigningRequestAsync(CreateCertificateSigningRequestWorkflowRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            var privateKeyEntity = await EnsureUnlockedPrivateKeyAsync(request.PrivateKeyId, cancellationToken);
            if (privateKeyEntity is null)
            {
                return OperationResult<StoredCertificateSigningRequestResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before CSR operations.");
            }

            var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
            var csrResult = await _certificateSigningRequestService.CreateAsync(
                new CreateCertificateSigningRequestRequest(request.SubjectName, decryptedPrivateKey, privateKeyEntity.Algorithm, request.SubjectAlternativeNames),
                cancellationToken);

            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);

            if (!csrResult.IsSuccess || csrResult.Value is null)
            {
                return OperationResult<StoredCertificateSigningRequestResult>.Failure(csrResult.ErrorCode, csrResult.Message);
            }

            var csrId = Guid.NewGuid();
            await _certificateRequestRepository.AddAsync(
                _currentDatabasePath!,
                new CertificateRequestEntity
                {
                    Id = csrId,
                    DisplayName = request.DisplayName,
                    Subject = csrResult.Value.Details.Subject,
                    PrivateKeyId = request.PrivateKeyId,
                    DerData = csrResult.Value.DerData,
                    DataFormat = CryptoDataFormat.Pkcs10.ToString(),
                    KeyAlgorithm = csrResult.Value.Details.KeyAlgorithm,
                    SubjectAlternativeNames = string.Join(";", csrResult.Value.Details.SubjectAlternativeNames),
                    CreatedUtc = DateTime.UtcNow
                },
                cancellationToken);

            await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.CertificateSigningRequestCreated, "Certificate signing request created."), cancellationToken);

            return OperationResult<StoredCertificateSigningRequestResult>.Success(
                new StoredCertificateSigningRequestResult(csrId, request.PrivateKeyId, csrResult.Value.Details),
                "Certificate signing request created.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<StoredCertificateResult>> SignCertificateSigningRequestAsync(SignStoredCertificateSigningRequestRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsUnlockedDatabase())
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before signing CSRs.");
            }

            var csrEntity = await _certificateRequestRepository.GetAsync(_currentDatabasePath!, request.CertificateSigningRequestId, cancellationToken);
            var issuerCertificate = await _certificateRepository.GetAsync(_currentDatabasePath!, request.IssuerCertificateId, cancellationToken);
            var issuerPrivateKey = await _privateKeyRepository.GetAsync(_currentDatabasePath!, request.IssuerPrivateKeyId, cancellationToken);

            if (csrEntity is null || issuerCertificate is null || issuerPrivateKey is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.DatabaseNotFound, "The requested CSR or issuer material could not be found.");
            }

            var decryptedIssuerKey = DecryptPrivateKey(issuerPrivateKey);
            var signResult = await _certificateService.SignCertificateSigningRequestAsync(
                new SignCertificateSigningRequestRequest(
                    csrEntity.DerData,
                    issuerCertificate.DerData,
                    decryptedIssuerKey,
                    issuerPrivateKey.Algorithm,
                    request.ValidityDays),
                cancellationToken);

            Array.Clear(decryptedIssuerKey, 0, decryptedIssuerKey.Length);

            if (!signResult.IsSuccess || signResult.Value is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(signResult.ErrorCode, signResult.Message);
            }

            var certificateId = Guid.NewGuid();
            Guid? subjectPrivateKeyId = csrEntity.PrivateKeyId == Guid.Empty ? null : csrEntity.PrivateKeyId;
            await _certificateRepository.AddAsync(
                _currentDatabasePath!,
                CreateCertificateEntity(certificateId, request.DisplayName, signResult.Value.DerData, signResult.Value.Details, subjectPrivateKeyId, request.IssuerCertificateId),
                cancellationToken);

            await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.CertificateSigningRequestSigned, "Certificate signing request signed."), cancellationToken);

            return OperationResult<StoredCertificateResult>.Success(
                new StoredCertificateResult(certificateId, subjectPrivateKeyId, signResult.Value.Details),
                "Certificate signing request signed.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<ImportStoredMaterialResult>> ImportStoredMaterialAsync(ImportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        if (!IsUnlockedDatabase())
        {
            return OperationResult<ImportStoredMaterialResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before importing material.");
        }

        var importResult = await _importExportService.ImportAsync(
            new ImportCertificateMaterialRequest(request.Kind, request.Format, request.Data, request.Password, request.DisplayName),
            cancellationToken);

        if (!importResult.IsSuccess || importResult.Value is null)
        {
            return OperationResult<ImportStoredMaterialResult>.Failure(importResult.ErrorCode, importResult.Message);
        }

        var privateKeyIds = new List<Guid>();
        var certificateIds = new List<Guid>();
        var csrIds = new List<Guid>();

        foreach (var privateKey in importResult.Value.PrivateKeys)
        {
            var storeResult = await StorePrivateKeyAsync(
                new StorePrivateKeyRequest(privateKey.DisplayName, privateKey.Algorithm, privateKey.PublicKeyFingerprint, privateKey.Pkcs8PrivateKey, "import"),
                cancellationToken);

            if (!storeResult.IsSuccess || storeResult.Value is null)
            {
                return OperationResult<ImportStoredMaterialResult>.Failure(storeResult.ErrorCode, storeResult.Message);
            }

            privateKeyIds.Add(storeResult.Value.PrivateKeyId);
        }

        foreach (var certificate in importResult.Value.Certificates)
        {
            var certificateId = Guid.NewGuid();
            var privateKeyId = privateKeyIds.Count == 1 ? privateKeyIds[0] : (Guid?)null;
            await _certificateRepository.AddAsync(_currentDatabasePath!, CreateCertificateEntity(certificateId, certificate.DisplayName, certificate.DerData, certificate.Details, privateKeyId, null), cancellationToken);
            await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.CertificateImported, "Certificate imported."), cancellationToken);
            certificateIds.Add(certificateId);
        }

        foreach (var csr in importResult.Value.CertificateSigningRequests)
        {
            var csrId = Guid.NewGuid();
            var privateKeyId = privateKeyIds.Count == 1 ? privateKeyIds[0] : Guid.Empty;
            await _certificateRequestRepository.AddAsync(
                _currentDatabasePath!,
                new CertificateRequestEntity
                {
                    Id = csrId,
                    DisplayName = csr.DisplayName,
                    Subject = csr.Details.Subject,
                    PrivateKeyId = privateKeyId,
                    DerData = csr.DerData,
                    DataFormat = CryptoDataFormat.Pkcs10.ToString(),
                    KeyAlgorithm = csr.Details.KeyAlgorithm,
                    SubjectAlternativeNames = string.Join(";", csr.Details.SubjectAlternativeNames),
                    CreatedUtc = DateTime.UtcNow
                },
                cancellationToken);

            csrIds.Add(csrId);
        }

        return OperationResult<ImportStoredMaterialResult>.Success(new ImportStoredMaterialResult(privateKeyIds, certificateIds, csrIds), "Material imported.");
    }

    public async Task<OperationResult<ExportedArtifact>> ExportStoredMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!IsUnlockedDatabase())
            {
                return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before exporting material.");
            }

            return request.Kind switch
            {
                CryptoImportKind.PrivateKey => await ExportPrivateKeyMaterialAsync(request, cancellationToken),
                CryptoImportKind.Certificate => await ExportCertificateMaterialAsync(request, cancellationToken),
                CryptoImportKind.CertificateSigningRequest => await ExportCertificateSigningRequestMaterialAsync(request, cancellationToken),
                _ => OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Unsupported export kind.")
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<CertificateDetails>> GetCertificateDetailsAsync(Guid certificateId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (string.IsNullOrWhiteSpace(_currentDatabasePath))
            {
                return OperationResult<CertificateDetails>.Failure(OperationErrorCode.DatabaseNotOpen, "No database is open.");
            }

            var certificateEntity = await _certificateRepository.GetAsync(_currentDatabasePath, certificateId, cancellationToken);
            if (certificateEntity is null)
            {
                return OperationResult<CertificateDetails>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate not found.");
            }

            return await _certificateService.ParseCertificateAsync(
                new CertificateParseRequest(certificateEntity.DerData, CryptoDataFormat.Der),
                cancellationToken);
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<DashboardSummary>> GetDashboardSummaryAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<DashboardSummary>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var certificates = await _certificateRepository.ListAsync(databasePath, cancellationToken);
            var privateKeys = await _privateKeyRepository.ListAsync(databasePath, cancellationToken);
            var certificateRequests = await _certificateRequestRepository.ListAsync(databasePath, cancellationToken);
            var certificateRevocationLists = await _certificateRevocationListRepository.ListAsync(databasePath, cancellationToken);
            var templates = await _templateRepository.ListAsync(databasePath, cancellationToken);

            return OperationResult<DashboardSummary>.Success(
                new DashboardSummary(
                    certificates.Count,
                    privateKeys.Count,
                    certificateRequests.Count,
                    certificateRevocationLists.Count,
                    templates.Count),
                "Dashboard summary loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<IReadOnlyList<CertificateListItem>>> ListCertificatesAsync(CertificateBrowserQuery query, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<IReadOnlyList<CertificateListItem>>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var now = DateTimeOffset.UtcNow;
            var expiringSoonBoundary = now.AddDays(Math.Max(1, query.ExpiringSoonWithinDays));
            var certificates = await _certificateRepository.ListAsync(databasePath, cancellationToken);
            var childCounts = certificates
                .Where(x => x.IssuerCertificateId.HasValue)
                .GroupBy(x => x.IssuerCertificateId!.Value)
                .ToDictionary(x => x.Key, x => x.Count());

            var filtered = certificates
                .Where(x => MatchesSearch(x, query.SearchText))
                .Where(x => MatchesAuthorityFilter(x, query.AuthorityFilter))
                .Where(x => MatchesValidityFilter(x, query.ValidityFilter, now, expiringSoonBoundary))
                .Select(x => new CertificateListItem(
                    x.Id,
                    x.DisplayName,
                    x.Subject,
                    x.Issuer,
                    x.SerialNumber,
                    x.Sha1Thumbprint,
                    x.Sha256Thumbprint,
                    ToDateTimeOffset(x.NotBeforeUtc),
                    ToDateTimeOffset(x.NotAfterUtc),
                    x.KeyAlgorithm,
                    x.IsCertificateAuthority,
                    FormatRevocationStatus(x.RevocationState),
                    x.IssuerCertificateId,
                    x.PrivateKeyId,
                    childCounts.GetValueOrDefault(x.Id)))
                .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .ToList();

            return OperationResult<IReadOnlyList<CertificateListItem>>.Success(filtered, "Certificates loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<CertificateInspector>> GetCertificateInspectorAsync(Guid certificateId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<CertificateInspector>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var certificates = await _certificateRepository.ListAsync(databasePath, cancellationToken);
            var certificateEntity = certificates.SingleOrDefault(x => x.Id == certificateId);
            if (certificateEntity is null)
            {
                return OperationResult<CertificateInspector>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate not found.");
            }

            var detailsResult = await _certificateService.ParseCertificateAsync(
                new CertificateParseRequest(certificateEntity.DerData, CryptoDataFormat.Der),
                cancellationToken);

            if (!detailsResult.IsSuccess || detailsResult.Value is null)
            {
                return OperationResult<CertificateInspector>.Failure(detailsResult.ErrorCode, detailsResult.Message);
            }

            var privateKeys = await _privateKeyRepository.ListAsync(databasePath, cancellationToken);
            var issuer = certificateEntity.IssuerCertificateId.HasValue
                ? certificates.SingleOrDefault(x => x.Id == certificateEntity.IssuerCertificateId.Value)
                : null;
            var relatedPrivateKey = certificateEntity.PrivateKeyId.HasValue
                ? privateKeys.SingleOrDefault(x => x.Id == certificateEntity.PrivateKeyId.Value)
                : null;
            var children = certificates
                .Where(x => x.IssuerCertificateId == certificateEntity.Id)
                .OrderBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
                .Select(x => new RelatedCertificateSummary(x.Id, x.DisplayName, x.Subject))
                .ToList();

            return OperationResult<CertificateInspector>.Success(
                new CertificateInspector(
                    certificateEntity.Id,
                    certificateEntity.DisplayName,
                    detailsResult.Value,
                    FormatRevocationStatus(certificateEntity.RevocationState),
                    issuer?.Id,
                    issuer?.DisplayName,
                    relatedPrivateKey?.Id,
                    relatedPrivateKey?.DisplayName,
                    children),
                "Certificate details loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<IReadOnlyList<PrivateKeyListItem>>> ListPrivateKeysAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<IReadOnlyList<PrivateKeyListItem>>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var certificates = await _certificateRepository.ListAsync(databasePath, cancellationToken);
            var linkedCertificateCounts = certificates
                .Where(x => x.PrivateKeyId.HasValue)
                .GroupBy(x => x.PrivateKeyId!.Value)
                .ToDictionary(x => x.Key, x => x.Count());
            var privateKeys = await _privateKeyRepository.ListAsync(databasePath, cancellationToken);

            var items = privateKeys
                .Select(x => new PrivateKeyListItem(
                    x.Id,
                    x.DisplayName,
                    x.Algorithm,
                    x.PublicKeyFingerprint,
                    DateTime.SpecifyKind(x.CreatedUtc, DateTimeKind.Utc),
                    linkedCertificateCounts.GetValueOrDefault(x.Id)))
                .ToList();

            return OperationResult<IReadOnlyList<PrivateKeyListItem>>.Success(items, "Private keys loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<IReadOnlyList<CertificateRequestListItem>>> ListCertificateSigningRequestsAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<IReadOnlyList<CertificateRequestListItem>>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var items = (await _certificateRequestRepository.ListAsync(databasePath, cancellationToken))
                .Select(x => new CertificateRequestListItem(
                    x.Id,
                    x.DisplayName,
                    x.Subject,
                    x.PrivateKeyId == Guid.Empty ? null : x.PrivateKeyId,
                    x.KeyAlgorithm,
                    x.SubjectAlternativeNames,
                    DateTime.SpecifyKind(x.CreatedUtc, DateTimeKind.Utc)))
                .ToList();

            return OperationResult<IReadOnlyList<CertificateRequestListItem>>.Success(items, "Certificate signing requests loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<IReadOnlyList<CertificateRevocationListItem>>> ListCertificateRevocationListsAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<IReadOnlyList<CertificateRevocationListItem>>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var items = (await _certificateRevocationListRepository.ListAsync(databasePath, cancellationToken))
                .Select(x => new CertificateRevocationListItem(
                    x.Id,
                    x.DisplayName,
                    x.AuthorityId,
                    DateTime.SpecifyKind(x.CreatedUtc, DateTimeKind.Utc),
                    x.NextUpdateUtc is null ? null : DateTime.SpecifyKind(x.NextUpdateUtc.Value, DateTimeKind.Utc)))
                .ToList();

            return OperationResult<IReadOnlyList<CertificateRevocationListItem>>.Success(items, "Certificate revocation lists loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<IReadOnlyList<TemplateListItem>>> ListTemplatesAsync(CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<IReadOnlyList<TemplateListItem>>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var items = (await _templateRepository.ListAsync(databasePath, cancellationToken))
                .Select(x => new TemplateListItem(x.Id, x.Name, x.Description, x.IsFavorite, x.IsDisabled))
                .ToList();

            return OperationResult<IReadOnlyList<TemplateListItem>>.Success(items, "Templates loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public DatabaseSessionSnapshot GetSnapshot()
    {
        var message = _state switch
        {
            DatabaseSessionState.Closed => "No database is open.",
            DatabaseSessionState.Locked => "Database is open and locked.",
            DatabaseSessionState.Unlocked => "Database is unlocked.",
            _ => "Unknown state."
        };

        return new DatabaseSessionSnapshot(
            _currentDatabasePath,
            _currentDisplayName,
            _state,
            XcaNetDbContext.CurrentSchemaVersion,
            _lastOpenedUtc,
            message);
    }

    public void Dispose()
    {
        ClearUnlockedKey();
        _gate.Dispose();
    }

    private static DatabaseProtectionProfile MapProfile(DatabaseProfileEntity entity)
    {
        return new DatabaseProtectionProfile(
            entity.KdfAlgorithm,
            entity.KdfIterations,
            entity.KdfSalt,
            entity.VerifierNonce,
            entity.VerifierCiphertext,
            entity.VerifierTag,
            entity.EncryptionAlgorithm,
            entity.KeyVersion);
    }

    private static AuditEventEntity CreateAuditEvent(string eventType, string message)
    {
        return new AuditEventEntity
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Message = message,
            OccurredUtc = DateTime.UtcNow
        };
    }

    private static string GetPrivateKeyAuditEvent(string source)
    {
        if (source.Equals("generated", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventKind.PrivateKeyGenerated;
        }

        if (source.Equals("import", StringComparison.OrdinalIgnoreCase))
        {
            return AuditEventKind.PrivateKeyImported;
        }

        return AuditEventKind.PrivateKeyStored;
    }

    private bool TryGetOpenDatabasePath<T>(out string databasePath, out OperationResult<T>? failure)
    {
        if (string.IsNullOrWhiteSpace(_currentDatabasePath))
        {
            databasePath = string.Empty;
            failure = OperationResult<T>.Failure(OperationErrorCode.DatabaseNotOpen, "Open a database before browsing its contents.");
            return false;
        }

        databasePath = _currentDatabasePath;
        failure = null;
        return true;
    }

    private bool IsUnlockedDatabase()
    {
        return !string.IsNullOrWhiteSpace(_currentDatabasePath) && _state == DatabaseSessionState.Unlocked && _unlockedKey is not null;
    }

    private async Task<PrivateKeyEntity?> EnsureUnlockedPrivateKeyAsync(Guid privateKeyId, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(_currentDatabasePath) || _state != DatabaseSessionState.Unlocked || _unlockedKey is null)
        {
            return null;
        }

        return await _privateKeyRepository.GetAsync(_currentDatabasePath, privateKeyId, cancellationToken);
    }

    private byte[] DecryptPrivateKey(PrivateKeyEntity privateKeyEntity)
    {
        var decryptResult = _databaseSecretProtector.DecryptPrivateKey(
            new EncryptedPrivateKeyPayload(
                privateKeyEntity.EncryptionNonce,
                privateKeyEntity.EncryptedPkcs8Ciphertext,
                privateKeyEntity.EncryptionTag,
                privateKeyEntity.EncryptionAlgorithm,
                privateKeyEntity.KeyVersion),
            _unlockedKey!);

        if (!decryptResult.IsSuccess || decryptResult.Value is null)
        {
            throw new InvalidOperationException(decryptResult.Message);
        }

        return decryptResult.Value;
    }

    private static CertificateEntity CreateCertificateEntity(
        Guid certificateId,
        string displayName,
        byte[] derData,
        CertificateDetails details,
        Guid? privateKeyId,
        Guid? issuerCertificateId)
    {
        return new CertificateEntity
        {
            Id = certificateId,
            DisplayName = displayName,
            Subject = details.Subject,
            Issuer = details.Issuer,
            SerialNumber = details.SerialNumber,
            Sha1Thumbprint = details.Sha1Thumbprint,
            Sha256Thumbprint = details.Sha256Thumbprint,
            NotBeforeUtc = details.NotBefore.UtcDateTime,
            NotAfterUtc = details.NotAfter.UtcDateTime,
            RevocationState = (int)RevocationState.Active,
            IssuerCertificateId = issuerCertificateId,
            PrivateKeyId = privateKeyId,
            DerData = derData,
            DataFormat = CryptoDataFormat.Der.ToString(),
            KeyAlgorithm = details.KeyAlgorithm,
            IsCertificateAuthority = details.IsCertificateAuthority
        };
    }

    private static bool MatchesSearch(CertificateEntity entity, string? searchText)
    {
        if (string.IsNullOrWhiteSpace(searchText))
        {
            return true;
        }

        return ContainsIgnoreCase(entity.DisplayName, searchText)
            || ContainsIgnoreCase(entity.Subject, searchText)
            || ContainsIgnoreCase(entity.Issuer, searchText)
            || ContainsIgnoreCase(entity.SerialNumber, searchText)
            || ContainsIgnoreCase(entity.Sha1Thumbprint, searchText)
            || ContainsIgnoreCase(entity.Sha256Thumbprint, searchText);
    }

    private static bool MatchesAuthorityFilter(CertificateEntity entity, CertificateAuthorityFilter filter)
    {
        return filter switch
        {
            CertificateAuthorityFilter.Authorities => entity.IsCertificateAuthority,
            CertificateAuthorityFilter.LeafCertificates => !entity.IsCertificateAuthority,
            _ => true
        };
    }

    private static bool MatchesValidityFilter(CertificateEntity entity, CertificateValidityFilter filter, DateTimeOffset now, DateTimeOffset expiringSoonBoundary)
    {
        if (filter == CertificateValidityFilter.All)
        {
            return true;
        }

        var notBefore = ToDateTimeOffset(entity.NotBeforeUtc);
        var notAfter = ToDateTimeOffset(entity.NotAfterUtc);
        var isRevoked = entity.RevocationState == (int)RevocationState.Revoked;
        var isExpired = notAfter.HasValue && notAfter.Value < now;
        var isValid = !isRevoked
            && !isExpired
            && (!notBefore.HasValue || notBefore.Value <= now)
            && (!notAfter.HasValue || notAfter.Value >= now);
        var isExpiringSoon = isValid && notAfter.HasValue && notAfter.Value <= expiringSoonBoundary;

        return filter switch
        {
            CertificateValidityFilter.Valid => isValid,
            CertificateValidityFilter.ExpiringSoon => isExpiringSoon,
            CertificateValidityFilter.Expired => isExpired,
            CertificateValidityFilter.Revoked => isRevoked,
            _ => true
        };
    }

    private static string FormatRevocationStatus(int revocationState)
    {
        return Enum.IsDefined(typeof(RevocationState), revocationState)
            ? ((RevocationState)revocationState).ToString()
            : RevocationState.Unknown.ToString();
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        return value is null
            ? null
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
    }

    private static bool ContainsIgnoreCase(string candidate, string searchText)
    {
        return candidate.Contains(searchText, StringComparison.OrdinalIgnoreCase);
    }

    private async Task<OperationResult<ExportedArtifact>> ExportPrivateKeyMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        var privateKeyEntity = await _privateKeyRepository.GetAsync(_currentDatabasePath!, request.MaterialId, cancellationToken);
        if (privateKeyEntity is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseNotFound, "Private key not found.");
        }

        var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
        try
        {
            var exportResult = await _keyService.ExportPrivateKeyAsync(
                new PrivateKeyExportRequest(request.FileNameStem, privateKeyEntity.Algorithm, decryptedPrivateKey, request.Format, request.Password),
                cancellationToken);

            if (exportResult.IsSuccess)
            {
                await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.PrivateKeyExported, "Private key exported."), cancellationToken);
            }

            return exportResult;
        }
        finally
        {
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
        }
    }

    private async Task<OperationResult<ExportedArtifact>> ExportCertificateMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        var certificateEntity = await _certificateRepository.GetAsync(_currentDatabasePath!, request.MaterialId, cancellationToken);
        if (certificateEntity is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate not found.");
        }

        OperationResult<ExportedArtifact> exportResult;

        if (request.Format == CryptoDataFormat.Pkcs12)
        {
            if (certificateEntity.PrivateKeyId is null)
            {
                return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "The certificate does not have an associated private key.");
            }

            var privateKeyEntity = await _privateKeyRepository.GetAsync(_currentDatabasePath!, certificateEntity.PrivateKeyId.Value, cancellationToken);
            if (privateKeyEntity is null)
            {
                return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseNotFound, "Associated private key not found.");
            }

            var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
            try
            {
                exportResult = await _importExportService.ExportPkcs12Async(
                    new ExportPkcs12Request(certificateEntity.DerData, decryptedPrivateKey, privateKeyEntity.Algorithm, request.FileNameStem, request.Password ?? string.Empty),
                    cancellationToken);
            }
            finally
            {
                Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
            }
        }
        else
        {
            exportResult = await _importExportService.ExportCertificateAsync(
                new ExportCertificateRequest(certificateEntity.DerData, request.Format, request.FileNameStem),
                cancellationToken);
        }

        if (exportResult.IsSuccess)
        {
            await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.CertificateExported, "Certificate exported."), cancellationToken);
        }

        return exportResult;
    }

    private async Task<OperationResult<ExportedArtifact>> ExportCertificateSigningRequestMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        var certificateRequestEntity = await _certificateRequestRepository.GetAsync(_currentDatabasePath!, request.MaterialId, cancellationToken);
        if (certificateRequestEntity is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate signing request not found.");
        }

        return await _importExportService.ExportCertificateSigningRequestAsync(
            new ExportCertificateSigningRequestRequest(certificateRequestEntity.DerData, request.Format, request.FileNameStem),
            cancellationToken);
    }

    private void ReplaceUnlockedKey(UnlockedDatabaseKey unlockedKey)
    {
        ClearUnlockedKey();
        _unlockedKey = unlockedKey;
    }

    private void ClearUnlockedKey()
    {
        _unlockedKey?.Dispose();
        _unlockedKey = null;
    }
}
