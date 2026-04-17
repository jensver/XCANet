using Microsoft.EntityFrameworkCore;
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
}
