using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface IAuditEventRepository
{
    Task AddAsync(string databasePath, AuditEventEntity auditEvent, CancellationToken cancellationToken);
}
