using Microsoft.EntityFrameworkCore;
using XcaNet.Contracts.Browser;
using XcaNet.Core.Enums;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class CertificateRepository : ICertificateRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public CertificateRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(string databasePath, CertificateEntity certificate, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        dbContext.Certificates.Add(certificate);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task<CertificateEntity?> GetAsync(string databasePath, Guid certificateId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.Certificates.SingleOrDefaultAsync(x => x.Id == certificateId, cancellationToken);
    }

    public async Task<IReadOnlyList<CertificateEntity>> ListAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.Certificates
            .AsNoTracking()
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task<IReadOnlyList<CertificateEntity>> ListAsync(string databasePath, CertificateFilterState filter, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var query = dbContext.Certificates.AsNoTracking().AsQueryable();
        var now = DateTime.UtcNow;
        var expiringSoonBoundary = now.AddDays(Math.Max(1, filter.ExpiringSoonWithinDays));

        if (!string.IsNullOrWhiteSpace(filter.DisplayName))
        {
            query = query.Where(x => EF.Functions.Like(x.DisplayName, $"%{filter.DisplayName}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Subject))
        {
            query = query.Where(x => EF.Functions.Like(x.Subject, $"%{filter.Subject}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Issuer))
        {
            query = query.Where(x => EF.Functions.Like(x.Issuer, $"%{filter.Issuer}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.SerialNumber))
        {
            query = query.Where(x => EF.Functions.Like(x.SerialNumber, $"%{filter.SerialNumber}%"));
        }

        if (!string.IsNullOrWhiteSpace(filter.Thumbprint))
        {
            query = query.Where(x =>
                EF.Functions.Like(x.Sha1Thumbprint, $"%{filter.Thumbprint}%")
                || EF.Functions.Like(x.Sha256Thumbprint, $"%{filter.Thumbprint}%"));
        }

        query = filter.AuthorityFilter switch
        {
            CertificateAuthorityFilter.Authorities => query.Where(x => x.IsCertificateAuthority),
            CertificateAuthorityFilter.LeafCertificates => query.Where(x => !x.IsCertificateAuthority),
            _ => query
        };

        query = filter.ValidityFilter switch
        {
            CertificateValidityFilter.Valid => query.Where(x =>
                x.RevocationState != (int)RevocationState.Revoked
                && (x.NotBeforeUtc == null || x.NotBeforeUtc <= now)
                && (x.NotAfterUtc == null || x.NotAfterUtc >= now)),
            CertificateValidityFilter.ExpiringSoon => query.Where(x =>
                x.RevocationState != (int)RevocationState.Revoked
                && (x.NotBeforeUtc == null || x.NotBeforeUtc <= now)
                && x.NotAfterUtc != null
                && x.NotAfterUtc >= now
                && x.NotAfterUtc <= expiringSoonBoundary),
            CertificateValidityFilter.Expired => query.Where(x => x.NotAfterUtc != null && x.NotAfterUtc < now),
            CertificateValidityFilter.Revoked => query.Where(x => x.RevocationState == (int)RevocationState.Revoked),
            _ => query
        };

        return await query
            .OrderBy(x => x.DisplayName)
            .ToListAsync(cancellationToken);
    }

    public async Task UpdateRevocationAsync(
        string databasePath,
        Guid certificateId,
        int revocationState,
        int? revocationReason,
        DateTime? revokedAtUtc,
        CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var certificate = await dbContext.Certificates.SingleAsync(x => x.Id == certificateId, cancellationToken);
        certificate.RevocationState = revocationState;
        certificate.RevocationReason = revocationReason;
        certificate.RevokedAtUtc = revokedAtUtc;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateDisplayNameAsync(string databasePath, Guid certificateId, string newName, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var certificate = await dbContext.Certificates.SingleAsync(x => x.Id == certificateId, cancellationToken);
        certificate.DisplayName = newName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    public async Task UpdateCommentAsync(string databasePath, Guid certificateId, string? comment, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var certificate = await dbContext.Certificates.SingleAsync(x => x.Id == certificateId, cancellationToken);
        certificate.Comment = comment;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
