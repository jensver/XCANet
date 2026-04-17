using Microsoft.Extensions.Logging;
using XcaNet.Contracts.Database;
using XcaNet.Contracts.Results;
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
    private readonly IPrivateKeyRepository _privateKeyRepository;
    private readonly IDatabaseSecretProtector _databaseSecretProtector;
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
        IPrivateKeyRepository privateKeyRepository,
        IDatabaseSecretProtector databaseSecretProtector,
        ILogger<DatabaseSessionService> logger)
    {
        _databaseMigrator = databaseMigrator;
        _databaseProfileRepository = databaseProfileRepository;
        _auditEventRepository = auditEventRepository;
        _privateKeyRepository = privateKeyRepository;
        _databaseSecretProtector = databaseSecretProtector;
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
                CreateAuditEvent(request.Source.Equals("import", StringComparison.OrdinalIgnoreCase) ? AuditEventKind.PrivateKeyImported : AuditEventKind.PrivateKeyStored, "Private key stored."),
                cancellationToken);

            return OperationResult<StorePrivateKeyResult>.Success(new StorePrivateKeyResult(privateKeyId), "Private key stored securely.");
        }
        finally
        {
            Array.Clear(request.Pkcs8Bytes, 0, request.Pkcs8Bytes.Length);
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
