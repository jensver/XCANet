using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class AuditEventRepository : IAuditEventRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public AuditEventRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task AddAsync(string databasePath, AuditEventEntity auditEvent, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        dbContext.AuditEvents.Add(auditEvent);
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
