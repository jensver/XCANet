using Microsoft.Extensions.Logging;
using System.Reflection;
using XcaNet.Contracts.Browser;
using XcaNet.Contracts.Crypto;
using XcaNet.Contracts.Crypto.Workflow;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Revocation;
using XcaNet.Contracts.Results;
using XcaNet.Crypto.Abstractions;
using XcaNet.Application.Templates;
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
    private readonly ICryptoBackendDiagnosticsProvider _cryptoBackendDiagnosticsProvider;
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
        ICryptoBackendDiagnosticsProvider cryptoBackendDiagnosticsProvider,
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
        _cryptoBackendDiagnosticsProvider = cryptoBackendDiagnosticsProvider;
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
            if (!TryGetUnlockedDatabasePath<StoredCertificateResult>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var privateKeyEntity = await EnsureUnlockedPrivateKeyAsync(request.PrivateKeyId, cancellationToken);
            if (privateKeyEntity is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before certificate operations.");
            }

            AppliedTemplateDefaults? appliedTemplate = null;
            if (request.TemplateId is not null)
            {
                var templateDefaultsResult = await ResolveTemplateDefaultsAsync(request.TemplateId.Value, TemplateWorkflowKind.SelfSignedCa, cancellationToken);
                if (!templateDefaultsResult.IsSuccess)
                {
                    return OperationResult<StoredCertificateResult>.Failure(templateDefaultsResult.ErrorCode, templateDefaultsResult.Message);
                }

                appliedTemplate = templateDefaultsResult.Value;
            }
            var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
            var createResult = await _certificateService.CreateSelfSignedCaAsync(
                new SelfSignedCaCertificateRequest(
                    string.IsNullOrWhiteSpace(request.SubjectName) ? appliedTemplate?.SubjectDefault ?? string.Empty : request.SubjectName,
                    decryptedPrivateKey,
                    privateKeyEntity.Algorithm,
                    request.ValidityDays > 0 ? request.ValidityDays : appliedTemplate?.ValidityDays ?? 3650,
                    appliedTemplate?.SubjectAlternativeNames.Select(x => new SanEntry(x)).ToArray(),
                    appliedTemplate?.IsCertificateAuthority ?? true,
                    appliedTemplate?.PathLengthConstraint,
                    appliedTemplate?.KeyUsages ?? [],
                    appliedTemplate?.EnhancedKeyUsages ?? []),
                cancellationToken);

            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);

            if (!createResult.IsSuccess || createResult.Value is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(createResult.ErrorCode, createResult.Message);
            }

            var certificateId = Guid.NewGuid();
            var displayName = string.IsNullOrWhiteSpace(request.DisplayName) ? appliedTemplate?.DisplayNameDefault ?? "Self-Signed CA" : request.DisplayName;
            await _certificateRepository.AddAsync(databasePath, CreateCertificateEntity(certificateId, displayName, createResult.Value.DerData, createResult.Value.Details, request.PrivateKeyId, null), cancellationToken);
            await _auditEventRepository.AddAsync(databasePath, CreateAuditEvent(AuditEventKind.CertificateCreated, "Certificate created."), cancellationToken);

            return OperationResult<StoredCertificateResult>.Success(
                new StoredCertificateResult(certificateId, request.PrivateKeyId, createResult.Value.Details, createResult.Value.BackendUsed),
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
            if (!TryGetUnlockedDatabasePath<StoredCertificateSigningRequestResult>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var privateKeyEntity = await EnsureUnlockedPrivateKeyAsync(request.PrivateKeyId, cancellationToken);
            if (privateKeyEntity is null)
            {
                return OperationResult<StoredCertificateSigningRequestResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before CSR operations.");
            }

            AppliedTemplateDefaults? appliedTemplate = null;
            if (request.TemplateId is not null)
            {
                var templateDefaultsResult = await ResolveTemplateDefaultsAsync(request.TemplateId.Value, TemplateWorkflowKind.CertificateSigningRequest, cancellationToken);
                if (!templateDefaultsResult.IsSuccess)
                {
                    return OperationResult<StoredCertificateSigningRequestResult>.Failure(templateDefaultsResult.ErrorCode, templateDefaultsResult.Message);
                }

                appliedTemplate = templateDefaultsResult.Value;
            }
            var subjectAlternativeNames = request.SubjectAlternativeNames.Count > 0
                ? request.SubjectAlternativeNames
                : appliedTemplate?.SubjectAlternativeNames.Select(x => new SanEntry(x)).ToArray() ?? [];
            var keyUsages = request.KeyUsages?.Count > 0
                ? request.KeyUsages
                : appliedTemplate?.KeyUsages ?? [];
            var enhancedKeyUsages = request.EnhancedKeyUsages?.Count > 0
                ? request.EnhancedKeyUsages
                : appliedTemplate?.EnhancedKeyUsages ?? [];
            var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
            var csrResult = await _certificateSigningRequestService.CreateAsync(
                new CreateCertificateSigningRequestRequest(
                    string.IsNullOrWhiteSpace(request.SubjectName) ? appliedTemplate?.SubjectDefault ?? string.Empty : request.SubjectName,
                    decryptedPrivateKey,
                    privateKeyEntity.Algorithm,
                    subjectAlternativeNames,
                    request.IsCertificateAuthority || appliedTemplate?.IsCertificateAuthority == true,
                    request.PathLengthConstraint ?? appliedTemplate?.PathLengthConstraint,
                    keyUsages,
                    enhancedKeyUsages),
                cancellationToken);

            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);

            if (!csrResult.IsSuccess || csrResult.Value is null)
            {
                return OperationResult<StoredCertificateSigningRequestResult>.Failure(csrResult.ErrorCode, csrResult.Message);
            }

            var csrId = Guid.NewGuid();
            await _certificateRequestRepository.AddAsync(
                databasePath,
                new CertificateRequestEntity
                {
                    Id = csrId,
                    DisplayName = string.IsNullOrWhiteSpace(request.DisplayName) ? appliedTemplate?.DisplayNameDefault ?? "Certificate Signing Request" : request.DisplayName,
                    Subject = csrResult.Value.Details.Subject,
                    PrivateKeyId = request.PrivateKeyId,
                    DerData = csrResult.Value.DerData,
                    DataFormat = CryptoDataFormat.Pkcs10.ToString(),
                    KeyAlgorithm = csrResult.Value.Details.KeyAlgorithm,
                    SubjectAlternativeNames = string.Join(";", csrResult.Value.Details.SubjectAlternativeNames),
                    CreatedUtc = DateTime.UtcNow
                },
                cancellationToken);

            await _auditEventRepository.AddAsync(databasePath, CreateAuditEvent(AuditEventKind.CertificateSigningRequestCreated, "Certificate signing request created."), cancellationToken);

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
            if (!TryGetUnlockedDatabasePath<StoredCertificateResult>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var csrEntity = await _certificateRequestRepository.GetAsync(databasePath, request.CertificateSigningRequestId, cancellationToken);
            var issuerCertificate = await _certificateRepository.GetAsync(databasePath, request.IssuerCertificateId, cancellationToken);
            var issuerPrivateKey = await _privateKeyRepository.GetAsync(databasePath, request.IssuerPrivateKeyId, cancellationToken);

            if (csrEntity is null || issuerCertificate is null || issuerPrivateKey is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.DatabaseNotFound, "The requested CSR or issuer material could not be found.");
            }

            AppliedTemplateDefaults? appliedTemplate = null;
            if (request.TemplateId is not null)
            {
                var templateDefaultsResult = await ResolveTemplateDefaultsAsync(request.TemplateId.Value, TemplateWorkflowKind.SignCertificateSigningRequest, cancellationToken);
                if (!templateDefaultsResult.IsSuccess)
                {
                    return OperationResult<StoredCertificateResult>.Failure(templateDefaultsResult.ErrorCode, templateDefaultsResult.Message);
                }

                appliedTemplate = templateDefaultsResult.Value;
            }
            var decryptedIssuerKey = DecryptPrivateKey(issuerPrivateKey);
            var signResult = await _certificateService.SignCertificateSigningRequestAsync(
                new SignCertificateSigningRequestRequest(
                    csrEntity.DerData,
                    issuerCertificate.DerData,
                    decryptedIssuerKey,
                    issuerPrivateKey.Algorithm,
                    request.ValidityDays > 0 ? request.ValidityDays : appliedTemplate?.ValidityDays ?? 365),
                cancellationToken);

            Array.Clear(decryptedIssuerKey, 0, decryptedIssuerKey.Length);

            if (!signResult.IsSuccess || signResult.Value is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(signResult.ErrorCode, signResult.Message);
            }

            var certificateId = Guid.NewGuid();
            Guid? subjectPrivateKeyId = csrEntity.PrivateKeyId == Guid.Empty ? null : csrEntity.PrivateKeyId;
            await _certificateRepository.AddAsync(
                databasePath,
                CreateCertificateEntity(
                    certificateId,
                    string.IsNullOrWhiteSpace(request.DisplayName) ? appliedTemplate?.DisplayNameDefault ?? "Issued Certificate" : request.DisplayName,
                    signResult.Value.DerData,
                    signResult.Value.Details,
                    subjectPrivateKeyId,
                    request.IssuerCertificateId),
                cancellationToken);

            await _auditEventRepository.AddAsync(databasePath, CreateAuditEvent(AuditEventKind.CertificateSigningRequestSigned, "Certificate signing request signed."), cancellationToken);

            return OperationResult<StoredCertificateResult>.Success(
                new StoredCertificateResult(certificateId, subjectPrivateKeyId, signResult.Value.Details, signResult.Value.BackendUsed),
                "Certificate signing request signed.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<StoredCertificateResult>> RevokeCertificateAsync(RevokeStoredCertificateRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetUnlockedDatabasePath<StoredCertificateResult>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var certificate = await _certificateRepository.GetAsync(databasePath, request.CertificateId, cancellationToken);
            if (certificate is null)
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate not found.");
            }

            if (certificate.RevocationState == (int)RevocationState.Revoked)
            {
                return OperationResult<StoredCertificateResult>.Failure(OperationErrorCode.ValidationFailed, "Certificate is already revoked.");
            }

            await _certificateRepository.UpdateRevocationAsync(
                databasePath,
                certificate.Id,
                (int)RevocationState.Revoked,
                (int)request.Reason,
                request.RevokedAt.UtcDateTime,
                cancellationToken);

            await _auditEventRepository.AddAsync(
                databasePath,
                CreateAuditEvent(
                    AuditEventKind.CertificateRevoked,
                    "Certificate revoked.",
                    "certificate",
                    certificate.Id),
                cancellationToken);

            var reloadedCertificate = await _certificateRepository.GetAsync(databasePath, certificate.Id, cancellationToken);
            var details = await _certificateService.ParseCertificateAsync(new CertificateParseRequest(reloadedCertificate!.DerData, CryptoDataFormat.Der), cancellationToken);

            return !details.IsSuccess || details.Value is null
                ? OperationResult<StoredCertificateResult>.Failure(details.ErrorCode, details.Message)
                : OperationResult<StoredCertificateResult>.Success(
                    new StoredCertificateResult(reloadedCertificate.Id, reloadedCertificate.PrivateKeyId, details.Value, CryptoBackendKind.Managed),
                    "Certificate revoked.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<StoredCertificateRevocationListResult>> GenerateCertificateRevocationListAsync(GenerateCertificateRevocationListWorkflowRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetUnlockedDatabasePath<StoredCertificateRevocationListResult>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var issuerCertificate = await _certificateRepository.GetAsync(databasePath, request.IssuerCertificateId, cancellationToken);
            var issuerPrivateKey = await _privateKeyRepository.GetAsync(databasePath, request.IssuerPrivateKeyId, cancellationToken);
            if (issuerCertificate is null || issuerPrivateKey is null)
            {
                return OperationResult<StoredCertificateRevocationListResult>.Failure(OperationErrorCode.DatabaseNotFound, "Issuer certificate or private key not found.");
            }

            if (!issuerCertificate.IsCertificateAuthority)
            {
                return OperationResult<StoredCertificateRevocationListResult>.Failure(OperationErrorCode.ValidationFailed, "Only CA certificates can issue CRLs.");
            }

            var revokedCertificates = (await _certificateRepository.ListAsync(databasePath, cancellationToken))
                .Where(x => x.IssuerCertificateId == request.IssuerCertificateId && x.RevocationState == (int)RevocationState.Revoked && x.RevocationReason.HasValue && x.RevokedAtUtc.HasValue)
                .Select(x => new RevokedCertificateEntry(
                    x.Id,
                    x.DisplayName,
                    x.Subject,
                    x.SerialNumber,
                    (CertificateRevocationReason)x.RevocationReason!.Value,
                    DateTime.SpecifyKind(x.RevokedAtUtc!.Value, DateTimeKind.Utc)))
                .OrderBy(x => x.RevokedAt)
                .ToList();

            var decryptedIssuerKey = DecryptPrivateKey(issuerPrivateKey);
            try
            {
                var currentCrlNumber = (await _certificateRevocationListRepository.ListAsync(databasePath, cancellationToken))
                    .Where(x => x.IssuerCertificateId == request.IssuerCertificateId)
                    .Select(x => x.CrlNumber)
                    .DefaultIfEmpty(0)
                    .Max();
                var nextCrlNumber = currentCrlNumber + 1;
                var thisUpdate = DateTimeOffset.UtcNow;
                var nextUpdate = thisUpdate.AddDays(Math.Max(1, request.NextUpdateDays));

                var crlResult = await _certificateService.GenerateCertificateRevocationListAsync(
                    new GenerateCertificateRevocationListRequest(
                        issuerCertificate.DerData,
                        decryptedIssuerKey,
                        issuerPrivateKey.Algorithm,
                        nextCrlNumber,
                        thisUpdate,
                        nextUpdate,
                        revokedCertificates),
                    cancellationToken);

                if (!crlResult.IsSuccess || crlResult.Value is null)
                {
                    return OperationResult<StoredCertificateRevocationListResult>.Failure(crlResult.ErrorCode, crlResult.Message);
                }

                var crlId = Guid.NewGuid();
                await _certificateRevocationListRepository.AddAsync(
                    databasePath,
                    new CertificateRevocationListEntity
                    {
                        Id = crlId,
                        DisplayName = request.DisplayName,
                        IssuerCertificateId = issuerCertificate.Id,
                        IssuerDisplayName = issuerCertificate.DisplayName,
                        CrlNumber = nextCrlNumber,
                        ThisUpdateUtc = crlResult.Value.Details.ThisUpdate.UtcDateTime,
                        NextUpdateUtc = crlResult.Value.Details.NextUpdate?.UtcDateTime,
                        DerData = crlResult.Value.DerData,
                        PemData = crlResult.Value.PemData,
                        RevokedEntries = revokedCertificates
                            .Select(x => new CertificateRevocationListEntryEntity
                            {
                                SerialNumber = x.SerialNumber,
                                DisplayName = x.DisplayName,
                                Subject = x.Subject,
                                Reason = (int)x.Reason,
                                RevokedAtUtc = x.RevokedAt.UtcDateTime
                            })
                            .ToList()
                    },
                    cancellationToken);

                await _auditEventRepository.AddAsync(
                    databasePath,
                    CreateAuditEvent(AuditEventKind.CertificateRevocationListGenerated, "Certificate revocation list generated.", "crl", crlId),
                    cancellationToken);

                return OperationResult<StoredCertificateRevocationListResult>.Success(
                    new StoredCertificateRevocationListResult(crlId, crlResult.Value.Details),
                    "Certificate revocation list generated.");
            }
            finally
            {
                Array.Clear(decryptedIssuerKey, 0, decryptedIssuerKey.Length);
            }
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

        if (request.Kind == CryptoImportKind.CertificateRevocationList)
        {
            var detailsResult = await _certificateService.ParseCertificateRevocationListAsync(
                new CertificateRevocationListParseRequest(request.Data, request.Format),
                cancellationToken);

            if (!detailsResult.IsSuccess || detailsResult.Value is null)
            {
                return OperationResult<ImportStoredMaterialResult>.Failure(detailsResult.ErrorCode, detailsResult.Message);
            }

            var pemData = request.Format == CryptoDataFormat.Pem ? System.Text.Encoding.UTF8.GetString(request.Data) : null;
            var crlId = await StoreCertificateRevocationListAsync(
                new ImportedCertificateRevocationListMaterial(request.DisplayName, request.Format == CryptoDataFormat.Pem ? GetPemBody(request.Data) : request.Data, pemData, detailsResult.Value),
                cancellationToken);

            return OperationResult<ImportStoredMaterialResult>.Success(new ImportStoredMaterialResult([], [], [], [crlId]), "CRL imported.");
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
        var crlIds = new List<Guid>();

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

        foreach (var certificateRevocationList in importResult.Value.CertificateRevocationLists)
        {
            var storedCrlId = await StoreCertificateRevocationListAsync(certificateRevocationList, cancellationToken);
            crlIds.Add(storedCrlId);
        }

        return OperationResult<ImportStoredMaterialResult>.Success(new ImportStoredMaterialResult(privateKeyIds, certificateIds, csrIds, crlIds), "Material imported.");
    }

    public async Task<OperationResult<ImportStoredFilesResult>> ImportStoredFilesAsync(ImportStoredFilesRequest request, CancellationToken cancellationToken)
    {
        if (!IsUnlockedDatabase())
        {
            return OperationResult<ImportStoredFilesResult>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before importing files.");
        }

        if (request.FilePaths.Count == 0)
        {
            return OperationResult<ImportStoredFilesResult>.Failure(OperationErrorCode.ValidationFailed, "Select at least one file to import.");
        }

        var importedFiles = new List<ImportedStoredFileItem>();

        foreach (var filePath in request.FilePaths)
        {
            if (string.IsNullOrWhiteSpace(filePath) || !File.Exists(filePath))
            {
                return OperationResult<ImportStoredFilesResult>.Failure(OperationErrorCode.ValidationFailed, $"The import file was not found: {filePath}");
            }

            OperationResult<ImportStoredMaterialRequest> classifyResult;
            try
            {
                classifyResult = await ClassifyImportFileAsync(filePath, request.Password, cancellationToken);
            }
            catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
            {
                return OperationResult<ImportStoredFilesResult>.Failure(
                    OperationErrorCode.StorageFailure,
                    $"Could not read import file '{Path.GetFileName(filePath)}': {ex.Message}");
            }

            if (!classifyResult.IsSuccess || classifyResult.Value is null)
            {
                return OperationResult<ImportStoredFilesResult>.Failure(classifyResult.ErrorCode, classifyResult.Message);
            }

            var importResult = await ImportStoredMaterialAsync(classifyResult.Value, cancellationToken);
            if (!importResult.IsSuccess || importResult.Value is null)
            {
                return OperationResult<ImportStoredFilesResult>.Failure(importResult.ErrorCode, importResult.Message);
            }

            importedFiles.Add(
                new ImportedStoredFileItem(
                    filePath,
                    classifyResult.Value.DisplayName,
                    classifyResult.Value.Kind,
                    importResult.Value.PrivateKeyIds,
                    importResult.Value.CertificateIds,
                    importResult.Value.CertificateSigningRequestIds,
                    importResult.Value.CertificateRevocationListIds));
        }

        return OperationResult<ImportStoredFilesResult>.Success(new ImportStoredFilesResult(importedFiles), "Files imported.");
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
                CryptoImportKind.CertificateRevocationList => await ExportCertificateRevocationListMaterialAsync(request, cancellationToken),
                _ => OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Unsupported export kind.")
            };
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<ExportedArtifact>> ExportStoredMaterialToFileAsync(ExportStoredMaterialToFileRequest request, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DestinationPath))
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Choose a destination path before exporting.");
        }

        var exportResult = await ExportStoredMaterialAsync(
            new ExportStoredMaterialRequest(request.Kind, request.MaterialId, request.Format, request.Password, request.FileNameStem, request.Mode),
            cancellationToken);

        if (!exportResult.IsSuccess || exportResult.Value is null)
        {
            return OperationResult<ExportedArtifact>.Failure(exportResult.ErrorCode, exportResult.Message);
        }

        try
        {
            Directory.CreateDirectory(Path.GetDirectoryName(request.DestinationPath) ?? ".");
            if (!string.IsNullOrWhiteSpace(exportResult.Value.TextRepresentation)
                && (exportResult.Value.Format == CryptoDataFormat.Pem || request.DestinationPath.EndsWith(".json", StringComparison.OrdinalIgnoreCase)))
            {
                await File.WriteAllTextAsync(request.DestinationPath, exportResult.Value.TextRepresentation, cancellationToken);
            }
            else
            {
                await File.WriteAllBytesAsync(request.DestinationPath, exportResult.Value.Data, cancellationToken);
            }
        }
        catch (Exception ex) when (ex is IOException or UnauthorizedAccessException or ArgumentException or NotSupportedException)
        {
            return OperationResult<ExportedArtifact>.Failure(
                OperationErrorCode.StorageFailure,
                $"Could not export '{request.FileNameStem}' to '{request.DestinationPath}': {ex.Message}");
        }

        return OperationResult<ExportedArtifact>.Success(exportResult.Value, "Material exported to file.");
    }

    public Task<OperationResult<ApplicationDiagnosticsSnapshot>> GetApplicationDiagnosticsAsync(CancellationToken cancellationToken)
    {
        cancellationToken.ThrowIfCancellationRequested();

        var appVersion = Assembly.GetEntryAssembly()?.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? Assembly.GetEntryAssembly()?.GetName().Version?.ToString()
            ?? typeof(DatabaseSessionService).Assembly.GetCustomAttribute<AssemblyInformationalVersionAttribute>()?.InformationalVersion
            ?? typeof(DatabaseSessionService).Assembly.GetName().Version?.ToString()
            ?? "0.0.0";

        return Task.FromResult(
            OperationResult<ApplicationDiagnosticsSnapshot>.Success(
                new ApplicationDiagnosticsSnapshot(
                    _cryptoBackendDiagnosticsProvider.GetSnapshot(),
                    XcaNetDbContext.CurrentSchemaVersion,
                    appVersion,
                    _state),
                "Application diagnostics loaded."));
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

    public async Task<OperationResult<IReadOnlyList<CertificateListItem>>> ListCertificatesAsync(CertificateFilterState filter, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<IReadOnlyList<CertificateListItem>>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var certificates = await _certificateRepository.ListAsync(databasePath, filter, cancellationToken);
            var allCertificates = await _certificateRepository.ListAsync(databasePath, cancellationToken);
            var childCounts = allCertificates
                .Where(x => x.IssuerCertificateId.HasValue)
                .GroupBy(x => x.IssuerCertificateId!.Value)
                .ToDictionary(x => x.Key, x => x.Count());

            var filtered = certificates
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
                    FormatRevocationReason(x.RevocationReason),
                    ToDateTimeOffset(x.RevokedAtUtc),
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

    public async Task<OperationResult<CertificateInspectorData>> GetCertificateInspectorAsync(Guid certificateId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<CertificateInspectorData>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var certificates = await _certificateRepository.ListAsync(databasePath, cancellationToken);
            var certificateEntity = certificates.SingleOrDefault(x => x.Id == certificateId);
            if (certificateEntity is null)
            {
                return OperationResult<CertificateInspectorData>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate not found.");
            }

            var detailsResult = await _certificateService.ParseCertificateAsync(
                new CertificateParseRequest(certificateEntity.DerData, CryptoDataFormat.Der),
                cancellationToken);

            if (!detailsResult.IsSuccess || detailsResult.Value is null)
            {
                return OperationResult<CertificateInspectorData>.Failure(detailsResult.ErrorCode, detailsResult.Message);
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
                .Select(x => new RelatedNavigationItem(
                    x.DisplayName,
                    x.Subject,
                    new NavigationTarget(BrowserEntityType.Certificate, x.Id, NavigationFocusSection.Inspector)))
                .ToList();

            return OperationResult<CertificateInspectorData>.Success(
                new CertificateInspectorData(
                    certificateEntity.Id,
                    new CertificateDisplayFields(
                        certificateEntity.DisplayName,
                        $"{detailsResult.Value.NotBefore:u} -> {detailsResult.Value.NotAfter:u}",
                        detailsResult.Value.IsCertificateAuthority ? "Certificate Authority" : "Leaf Certificate",
                        issuer?.DisplayName ?? detailsResult.Value.Issuer,
                        relatedPrivateKey?.DisplayName),
                    new CertificateRawFields(
                        detailsResult.Value.Subject,
                        detailsResult.Value.Issuer,
                        detailsResult.Value.SerialNumber,
                        detailsResult.Value.NotBefore,
                        detailsResult.Value.NotAfter,
                        detailsResult.Value.Sha1Thumbprint,
                        detailsResult.Value.Sha256Thumbprint,
                        detailsResult.Value.KeyAlgorithm),
                    new CertificateExtensionFields(
                        detailsResult.Value.IsCertificateAuthority,
                        detailsResult.Value.SubjectAlternativeNames,
                        detailsResult.Value.KeyUsages,
                        detailsResult.Value.EnhancedKeyUsages),
                    new CertificateRevocationInfo(
                        certificateEntity.RevocationState == (int)RevocationState.Revoked,
                        FormatRevocationStatus(certificateEntity.RevocationState),
                        certificateEntity.RevocationReason.HasValue ? (CertificateRevocationReason)certificateEntity.RevocationReason.Value : null,
                        ToDateTimeOffset(certificateEntity.RevokedAtUtc),
                        FormatRevocationReason(certificateEntity.RevocationReason)),
                    new CertificateNavigationInfo(
                        issuer is null ? null : new NavigationTarget(BrowserEntityType.Certificate, issuer.Id, NavigationFocusSection.Inspector),
                        relatedPrivateKey is null ? null : new NavigationTarget(BrowserEntityType.PrivateKey, relatedPrivateKey.Id, NavigationFocusSection.Overview),
                        children)),
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
                    x.PrivateKeyId == Guid.Empty ? null : new NavigationTarget(BrowserEntityType.PrivateKey, x.PrivateKeyId, NavigationFocusSection.Overview),
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
                    x.IssuerCertificateId,
                    x.IssuerDisplayName,
                    x.CrlNumber.ToString(),
                    DateTime.SpecifyKind(x.ThisUpdateUtc, DateTimeKind.Utc),
                    x.NextUpdateUtc is null ? null : DateTime.SpecifyKind(x.NextUpdateUtc.Value, DateTimeKind.Utc),
                    x.RevokedEntries.Count,
                    x.IssuerCertificateId == Guid.Empty ? null : new NavigationTarget(BrowserEntityType.Certificate, x.IssuerCertificateId, NavigationFocusSection.Inspector)))
                .OrderByDescending(x => x.ThisUpdate)
                .ToList();

            return OperationResult<IReadOnlyList<CertificateRevocationListItem>>.Success(items, "Certificate revocation lists loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<CertificateRevocationListInspectorData>> GetCertificateRevocationListInspectorAsync(Guid certificateRevocationListId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<CertificateRevocationListInspectorData>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var crl = await _certificateRevocationListRepository.GetAsync(databasePath, certificateRevocationListId, cancellationToken);
            if (crl is null)
            {
                return OperationResult<CertificateRevocationListInspectorData>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate revocation list not found.");
            }

            return OperationResult<CertificateRevocationListInspectorData>.Success(
                new CertificateRevocationListInspectorData(
                    crl.Id,
                    crl.DisplayName,
                    crl.IssuerDisplayName,
                    crl.IssuerCertificateId == Guid.Empty ? null : new NavigationTarget(BrowserEntityType.Certificate, crl.IssuerCertificateId, NavigationFocusSection.Inspector),
                    crl.CrlNumber.ToString(),
                    DateTime.SpecifyKind(crl.ThisUpdateUtc, DateTimeKind.Utc),
                    crl.NextUpdateUtc is null ? null : DateTime.SpecifyKind(crl.NextUpdateUtc.Value, DateTimeKind.Utc),
                    crl.RevokedEntries
                        .OrderBy(x => x.RevokedAtUtc)
                        .Select(x => new RevokedCertificateEntry(
                            Guid.Empty,
                            x.DisplayName,
                            x.Subject,
                            x.SerialNumber,
                            (CertificateRevocationReason)x.Reason,
                            DateTime.SpecifyKind(x.RevokedAtUtc, DateTimeKind.Utc)))
                        .ToList()),
                "Certificate revocation list details loaded.");
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
                .Select(TemplateModelMapper.ToListItem)
                .ToList();

            return OperationResult<IReadOnlyList<TemplateListItem>>.Success(items, "Templates loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<TemplateDetails>> GetTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<TemplateDetails>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var template = await _templateRepository.GetAsync(databasePath, templateId, cancellationToken);
            return template is null
                ? OperationResult<TemplateDetails>.Failure(OperationErrorCode.DatabaseNotFound, "Template not found.")
                : OperationResult<TemplateDetails>.Success(TemplateModelMapper.ToDetails(template), "Template loaded.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<TemplateDetails>> SaveTemplateAsync(SaveTemplateRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<TemplateDetails>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var templateId = request.TemplateId ?? Guid.NewGuid();
            var entity = TemplateModelMapper.ToEntity(request, templateId);
            var validation = TemplateModelMapper.Validate(entity);
            if (validation.Errors.Count > 0)
            {
                return OperationResult<TemplateDetails>.Failure(OperationErrorCode.ValidationFailed, string.Join(" ", validation.Errors));
            }

            var saved = await _templateRepository.SaveAsync(databasePath, entity, cancellationToken);
            return OperationResult<TemplateDetails>.Success(TemplateModelMapper.ToDetails(saved), request.TemplateId is null ? "Template created." : "Template updated.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<TemplateDetails>> CloneTemplateAsync(CloneTemplateRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<TemplateDetails>(out var databasePath, out var failure))
            {
                return failure!;
            }

            var clone = await _templateRepository.CloneAsync(databasePath, request.TemplateId, request.NewName, cancellationToken);
            return clone is null
                ? OperationResult<TemplateDetails>.Failure(OperationErrorCode.DatabaseNotFound, "Template not found.")
                : OperationResult<TemplateDetails>.Success(TemplateModelMapper.ToDetails(clone), "Template cloned.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<TemplateDetails>> SetTemplateFavoriteAsync(SetTemplateFavoriteRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<TemplateDetails>(out var databasePath, out var failure))
            {
                return failure!;
            }

            if (!await _templateRepository.SetFavoriteAsync(databasePath, request.TemplateId, request.IsFavorite, cancellationToken))
            {
                return OperationResult<TemplateDetails>.Failure(OperationErrorCode.DatabaseNotFound, "Template not found.");
            }

            var template = await _templateRepository.GetAsync(databasePath, request.TemplateId, cancellationToken);
            return OperationResult<TemplateDetails>.Success(TemplateModelMapper.ToDetails(template!), "Template favorite state updated.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<TemplateDetails>> SetTemplateEnabledAsync(SetTemplateEnabledRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<TemplateDetails>(out var databasePath, out var failure))
            {
                return failure!;
            }

            if (!await _templateRepository.SetEnabledAsync(databasePath, request.TemplateId, request.IsEnabled, cancellationToken))
            {
                return OperationResult<TemplateDetails>.Failure(OperationErrorCode.DatabaseNotFound, "Template not found.");
            }

            var template = await _templateRepository.GetAsync(databasePath, request.TemplateId, cancellationToken);
            return OperationResult<TemplateDetails>.Success(TemplateModelMapper.ToDetails(template!), "Template enabled state updated.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult> DeleteTemplateAsync(Guid templateId, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<object>(out var databasePath, out var failure))
            {
                return OperationResult.Failure(failure!.ErrorCode, failure.Message);
            }

            return await _templateRepository.DeleteAsync(databasePath, templateId, cancellationToken)
                ? OperationResult.Success("Template deleted.")
                : OperationResult.Failure(OperationErrorCode.DatabaseNotFound, "Template not found.");
        }
        finally
        {
            _gate.Release();
        }
    }

    public async Task<OperationResult<AppliedTemplateDefaults>> ApplyTemplateAsync(ApplyTemplateRequest request, CancellationToken cancellationToken)
    {
        await _gate.WaitAsync(cancellationToken);
        try
        {
            if (!TryGetOpenDatabasePath<AppliedTemplateDefaults>(out var databasePath, out var failure))
            {
                return failure!;
            }

            return await ResolveTemplateDefaultsAsync(request.TemplateId, request.Workflow, cancellationToken);
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

    private static AuditEventEntity CreateAuditEvent(string eventType, string message, string? entityType = null, Guid? entityId = null)
    {
        return new AuditEventEntity
        {
            Id = Guid.NewGuid(),
            EventType = eventType,
            Message = message,
            EntityType = entityType,
            EntityId = entityId,
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

    private async Task<OperationResult<AppliedTemplateDefaults>> ResolveTemplateDefaultsAsync(Guid templateId, TemplateWorkflowKind workflow, CancellationToken cancellationToken)
    {
        if (templateId == Guid.Empty)
        {
            return OperationResult<AppliedTemplateDefaults>.Failure(OperationErrorCode.ValidationFailed, "Template selection is required.");
        }

        if (string.IsNullOrWhiteSpace(_currentDatabasePath))
        {
            return OperationResult<AppliedTemplateDefaults>.Failure(OperationErrorCode.DatabaseNotOpen, "Open a database before using templates.");
        }

        var template = await _templateRepository.GetAsync(_currentDatabasePath, templateId, cancellationToken);
        if (template is null)
        {
            return OperationResult<AppliedTemplateDefaults>.Failure(OperationErrorCode.DatabaseNotFound, "Template not found.");
        }

        var validation = TemplateModelMapper.Validate(template);
        if (validation.Errors.Count > 0)
        {
            return OperationResult<AppliedTemplateDefaults>.Failure(OperationErrorCode.ValidationFailed, string.Join(" ", validation.Errors));
        }

        var compatibilityFailure = TemplateModelMapper.ValidateWorkflowCompatibility(template, workflow);
        if (!string.IsNullOrWhiteSpace(compatibilityFailure))
        {
            return OperationResult<AppliedTemplateDefaults>.Failure(OperationErrorCode.ValidationFailed, compatibilityFailure);
        }

        return OperationResult<AppliedTemplateDefaults>.Success(TemplateModelMapper.ToAppliedDefaults(template, workflow), "Template defaults applied.");
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

    private bool TryGetUnlockedDatabasePath<T>(out string databasePath, out OperationResult<T>? failure)
    {
        if (!TryGetOpenDatabasePath(out databasePath, out failure))
        {
            return false;
        }

        if (!IsUnlockedDatabase())
        {
            failure = OperationResult<T>.Failure(OperationErrorCode.DatabaseLocked, "Unlock the database before performing this action.");
            return false;
        }

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

    private static string FormatRevocationStatus(int revocationState)
    {
        return Enum.IsDefined(typeof(RevocationState), revocationState)
            ? ((RevocationState)revocationState).ToString()
            : RevocationState.Unknown.ToString();
    }

    private static string? FormatRevocationReason(int? revocationReason)
    {
        return revocationReason.HasValue && Enum.IsDefined(typeof(CertificateRevocationReason), revocationReason.Value)
            ? ((CertificateRevocationReason)revocationReason.Value).ToString()
            : null;
    }

    private static DateTimeOffset? ToDateTimeOffset(DateTime? value)
    {
        return value is null
            ? null
            : DateTime.SpecifyKind(value.Value, DateTimeKind.Utc);
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

        if (request.Mode == StoredMaterialExportMode.CertificateChain)
        {
            exportResult = await ExportCertificateChainAsync(certificateEntity, request, cancellationToken);
        }
        else if (request.Mode == StoredMaterialExportMode.CertificateWithPrivateKeyBundle && request.Format != CryptoDataFormat.Pkcs12)
        {
            exportResult = await ExportCertificateWithPrivateKeyBundleAsync(certificateEntity, request, cancellationToken);
        }
        else if (request.Format == CryptoDataFormat.Pkcs12)
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

    private async Task<OperationResult<ExportedArtifact>> ExportCertificateRevocationListMaterialAsync(ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        var certificateRevocationList = await _certificateRevocationListRepository.GetAsync(_currentDatabasePath!, request.MaterialId, cancellationToken);
        if (certificateRevocationList is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseNotFound, "Certificate revocation list not found.");
        }

        var pemData = certificateRevocationList.PemData ?? System.Security.Cryptography.PemEncoding.WriteString("X509 CRL", certificateRevocationList.DerData);
        ExportedArtifact? artifact = request.Format switch
        {
            CryptoDataFormat.Pem => new ExportedArtifact(
                CryptoDataFormat.Pem,
                System.Text.Encoding.UTF8.GetBytes(pemData),
                pemData,
                "application/pkix-crl",
                $"{request.FileNameStem}.crl.pem"),
            CryptoDataFormat.Der => new ExportedArtifact(
                CryptoDataFormat.Der,
                certificateRevocationList.DerData,
                null,
                "application/pkix-crl",
                $"{request.FileNameStem}.crl"),
            _ => null
        };

        if (artifact is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Unsupported CRL export format.");
        }

        return OperationResult<ExportedArtifact>.Success(artifact, "Certificate revocation list exported.");
    }

    private async Task<OperationResult<ExportedArtifact>> ExportCertificateChainAsync(CertificateEntity certificateEntity, ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        if (request.Format != CryptoDataFormat.Pem)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Certificate chain export currently supports PEM only.");
        }

        var certificates = await _certificateRepository.ListAsync(_currentDatabasePath!, cancellationToken);
        var chain = new List<CertificateEntity> { certificateEntity };
        var current = certificateEntity;
        while (current.IssuerCertificateId.HasValue)
        {
            var issuer = certificates.SingleOrDefault(x => x.Id == current.IssuerCertificateId.Value);
            if (issuer is null)
            {
                break;
            }

            chain.Add(issuer);
            current = issuer;
        }

        var parts = new List<string>();
        foreach (var item in chain)
        {
            var exportResult = await _importExportService.ExportCertificateAsync(
                new ExportCertificateRequest(item.DerData, CryptoDataFormat.Pem, request.FileNameStem),
                cancellationToken);

            if (!exportResult.IsSuccess || exportResult.Value?.TextRepresentation is null)
            {
                return OperationResult<ExportedArtifact>.Failure(exportResult.ErrorCode, exportResult.Message);
            }

            parts.Add(exportResult.Value.TextRepresentation.Trim());
        }

        var pem = string.Join(Environment.NewLine + Environment.NewLine, parts) + Environment.NewLine;
        return OperationResult<ExportedArtifact>.Success(
            new ExportedArtifact(CryptoDataFormat.Pem, System.Text.Encoding.UTF8.GetBytes(pem), pem, "application/x-pem-file", $"{request.FileNameStem}-chain.pem"),
            "Certificate chain exported.");
    }

    private async Task<OperationResult<ExportedArtifact>> ExportCertificateWithPrivateKeyBundleAsync(CertificateEntity certificateEntity, ExportStoredMaterialRequest request, CancellationToken cancellationToken)
    {
        if (request.Format != CryptoDataFormat.Pem)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "Certificate and private key bundle export currently supports PEM only.");
        }

        if (certificateEntity.PrivateKeyId is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.ValidationFailed, "The certificate does not have an associated private key.");
        }

        var privateKeyEntity = await _privateKeyRepository.GetAsync(_currentDatabasePath!, certificateEntity.PrivateKeyId.Value, cancellationToken);
        if (privateKeyEntity is null)
        {
            return OperationResult<ExportedArtifact>.Failure(OperationErrorCode.DatabaseNotFound, "Associated private key not found.");
        }

        var certificateExport = await _importExportService.ExportCertificateAsync(
            new ExportCertificateRequest(certificateEntity.DerData, CryptoDataFormat.Pem, request.FileNameStem),
            cancellationToken);
        if (!certificateExport.IsSuccess || certificateExport.Value?.TextRepresentation is null)
        {
            return OperationResult<ExportedArtifact>.Failure(certificateExport.ErrorCode, certificateExport.Message);
        }

        var decryptedPrivateKey = DecryptPrivateKey(privateKeyEntity);
        try
        {
            var keyExport = await _keyService.ExportPrivateKeyAsync(
                new PrivateKeyExportRequest(request.FileNameStem, privateKeyEntity.Algorithm, decryptedPrivateKey, CryptoDataFormat.Pem, request.Password),
                cancellationToken);

            if (!keyExport.IsSuccess || keyExport.Value?.TextRepresentation is null)
            {
                return OperationResult<ExportedArtifact>.Failure(keyExport.ErrorCode, keyExport.Message);
            }

            var bundle = certificateExport.Value.TextRepresentation.Trim()
                + Environment.NewLine + Environment.NewLine
                + keyExport.Value.TextRepresentation.Trim()
                + Environment.NewLine;

            await _auditEventRepository.AddAsync(_currentDatabasePath!, CreateAuditEvent(AuditEventKind.PrivateKeyExported, "Private key exported."), cancellationToken);
            return OperationResult<ExportedArtifact>.Success(
                new ExportedArtifact(CryptoDataFormat.Pem, System.Text.Encoding.UTF8.GetBytes(bundle), bundle, "application/x-pem-file", $"{request.FileNameStem}-bundle.pem"),
                "Certificate and private key bundle exported.");
        }
        finally
        {
            Array.Clear(decryptedPrivateKey, 0, decryptedPrivateKey.Length);
        }
    }

    private async Task<Guid> StoreCertificateRevocationListAsync(ImportedCertificateRevocationListMaterial certificateRevocationList, CancellationToken cancellationToken)
    {
        var certificates = await _certificateRepository.ListAsync(_currentDatabasePath!, cancellationToken);
        var issuer = certificates
            .OrderBy(x => x.Subject.Equals(certificateRevocationList.Details.Issuer, StringComparison.OrdinalIgnoreCase) ? 0 : 1)
            .ThenBy(x => x.DisplayName, StringComparer.OrdinalIgnoreCase)
            .FirstOrDefault(x => x.Subject.Equals(certificateRevocationList.Details.Issuer, StringComparison.OrdinalIgnoreCase));

        var crlId = Guid.NewGuid();
        await _certificateRevocationListRepository.AddAsync(
            _currentDatabasePath!,
            new CertificateRevocationListEntity
            {
                Id = crlId,
                DisplayName = certificateRevocationList.DisplayName,
                IssuerCertificateId = issuer?.Id ?? Guid.Empty,
                IssuerDisplayName = issuer?.DisplayName ?? certificateRevocationList.Details.Issuer,
                CrlNumber = long.TryParse(certificateRevocationList.Details.CrlNumber, out var parsedCrlNumber) ? parsedCrlNumber : 0,
                ThisUpdateUtc = certificateRevocationList.Details.ThisUpdate.UtcDateTime,
                NextUpdateUtc = certificateRevocationList.Details.NextUpdate?.UtcDateTime,
                DerData = certificateRevocationList.DerData,
                PemData = certificateRevocationList.PemData,
                RevokedEntries = certificateRevocationList.Details.RevokedCertificates
                    .Select(x => new CertificateRevocationListEntryEntity
                    {
                        SerialNumber = x.SerialNumber,
                        DisplayName = x.DisplayName,
                        Subject = x.Subject,
                        Reason = (int)x.Reason,
                        RevokedAtUtc = x.RevokedAt.UtcDateTime
                    })
                    .ToList()
            },
            cancellationToken);

        return crlId;
    }

    private async Task<OperationResult<ImportStoredMaterialRequest>> ClassifyImportFileAsync(string filePath, string? password, CancellationToken cancellationToken)
    {
        var data = await File.ReadAllBytesAsync(filePath, cancellationToken);
        var displayName = Path.GetFileNameWithoutExtension(filePath);
        var extension = Path.GetExtension(filePath);

        if (data.Length == 0)
        {
            return OperationResult<ImportStoredMaterialRequest>.Failure(OperationErrorCode.ValidationFailed, $"The selected import file is empty: {Path.GetFileName(filePath)}");
        }

        if (LooksLikePem(data, "X509 CRL") || extension.Equals(".crl", StringComparison.OrdinalIgnoreCase))
        {
            var format = LooksLikePem(data, "X509 CRL") ? CryptoDataFormat.Pem : CryptoDataFormat.Der;
            return OperationResult<ImportStoredMaterialRequest>.Success(
                new ImportStoredMaterialRequest(displayName, CryptoImportKind.CertificateRevocationList, format, data, password),
                "CRL import classified.");
        }

        if (LooksLikePem(data, "CERTIFICATE REQUEST") || extension.Equals(".csr", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<ImportStoredMaterialRequest>.Success(
                new ImportStoredMaterialRequest(displayName, CryptoImportKind.CertificateSigningRequest, LooksLikePem(data, "CERTIFICATE REQUEST") ? CryptoDataFormat.Pem : CryptoDataFormat.Pkcs10, data, password),
                "CSR import classified.");
        }

        if (LooksLikePem(data, "CERTIFICATE"))
        {
            return OperationResult<ImportStoredMaterialRequest>.Success(
                new ImportStoredMaterialRequest(displayName, CryptoImportKind.Certificate, CryptoDataFormat.Pem, data, password),
                "Certificate import classified.");
        }

        if (LooksLikePem(data, "PRIVATE KEY") || extension.Equals(".key", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<ImportStoredMaterialRequest>.Success(
                new ImportStoredMaterialRequest(displayName, CryptoImportKind.PrivateKey, LooksLikePem(data, "PRIVATE KEY") ? CryptoDataFormat.Pem : CryptoDataFormat.Pkcs8, data, password),
                "Private key import classified.");
        }

        if (extension.Equals(".pfx", StringComparison.OrdinalIgnoreCase) || extension.Equals(".p12", StringComparison.OrdinalIgnoreCase))
        {
            return OperationResult<ImportStoredMaterialRequest>.Success(
                new ImportStoredMaterialRequest(displayName, CryptoImportKind.Bundle, CryptoDataFormat.Pkcs12, data, password),
                "Bundle import classified.");
        }

        if (extension.Equals(".der", StringComparison.OrdinalIgnoreCase) || extension.Equals(".cer", StringComparison.OrdinalIgnoreCase))
        {
            if (await CanParseCertificateAsync(data, cancellationToken))
            {
                return OperationResult<ImportStoredMaterialRequest>.Success(
                    new ImportStoredMaterialRequest(displayName, CryptoImportKind.Certificate, CryptoDataFormat.Der, data, password),
                    "Certificate import classified.");
            }

            if (await CanParseCertificateSigningRequestAsync(data, cancellationToken))
            {
                return OperationResult<ImportStoredMaterialRequest>.Success(
                    new ImportStoredMaterialRequest(displayName, CryptoImportKind.CertificateSigningRequest, CryptoDataFormat.Pkcs10, data, password),
                    "CSR import classified.");
            }

            if (await CanParseCertificateRevocationListAsync(data, cancellationToken))
            {
                return OperationResult<ImportStoredMaterialRequest>.Success(
                    new ImportStoredMaterialRequest(displayName, CryptoImportKind.CertificateRevocationList, CryptoDataFormat.Der, data, password),
                    "CRL import classified.");
            }

            return OperationResult<ImportStoredMaterialRequest>.Failure(
                OperationErrorCode.ValidationFailed,
                $"The file '{Path.GetFileName(filePath)}' could not be recognized as a certificate, CSR, or CRL.");
        }

        return OperationResult<ImportStoredMaterialRequest>.Failure(
            OperationErrorCode.ValidationFailed,
            $"Unsupported file type: {Path.GetFileName(filePath)}. Supported imports include PEM, DER/CER, KEY, CSR, CRL, PFX, and P12.");
    }

    private async Task<bool> CanParseCertificateAsync(byte[] data, CancellationToken cancellationToken)
    {
        var result = await _certificateService.ParseCertificateAsync(new CertificateParseRequest(data, CryptoDataFormat.Der), cancellationToken);
        return result.IsSuccess;
    }

    private async Task<bool> CanParseCertificateSigningRequestAsync(byte[] data, CancellationToken cancellationToken)
    {
        var result = await _certificateSigningRequestService.ParseAsync(new CertificateSigningRequestParseRequest(data, CryptoDataFormat.Der), cancellationToken);
        return result.IsSuccess;
    }

    private async Task<bool> CanParseCertificateRevocationListAsync(byte[] data, CancellationToken cancellationToken)
    {
        var result = await _certificateService.ParseCertificateRevocationListAsync(new CertificateRevocationListParseRequest(data, CryptoDataFormat.Der), cancellationToken);
        return result.IsSuccess;
    }

    private static bool LooksLikePem(byte[] data, string label)
    {
        if (data.Length == 0)
        {
            return false;
        }

        var text = System.Text.Encoding.UTF8.GetString(data);
        return text.Contains($"BEGIN {label}", StringComparison.Ordinal);
    }

    private static byte[] GetPemBody(byte[] data)
    {
        var text = System.Text.Encoding.UTF8.GetString(data);
        var pemField = System.Security.Cryptography.PemEncoding.Find(text);
        return Convert.FromBase64String(text[pemField.Base64Data].ToString());
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
