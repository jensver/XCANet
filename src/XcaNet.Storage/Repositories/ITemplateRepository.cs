using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ITemplateRepository
{
    Task<IReadOnlyList<TemplateEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);
}
