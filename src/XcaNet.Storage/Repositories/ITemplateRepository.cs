using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public interface ITemplateRepository
{
    Task<IReadOnlyList<TemplateEntity>> ListAsync(string databasePath, CancellationToken cancellationToken);
    Task<TemplateEntity?> GetAsync(string databasePath, Guid templateId, CancellationToken cancellationToken);
    Task<TemplateEntity> SaveAsync(string databasePath, TemplateEntity template, CancellationToken cancellationToken);
    Task<TemplateEntity?> CloneAsync(string databasePath, Guid templateId, string? newName, CancellationToken cancellationToken);
    Task<bool> SetFavoriteAsync(string databasePath, Guid templateId, bool isFavorite, CancellationToken cancellationToken);
    Task<bool> SetEnabledAsync(string databasePath, Guid templateId, bool isEnabled, CancellationToken cancellationToken);
    Task<bool> DeleteAsync(string databasePath, Guid templateId, CancellationToken cancellationToken);
}
