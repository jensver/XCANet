using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Repositories;

public sealed class TemplateRepository : ITemplateRepository
{
    private readonly IXcaNetDbContextFactory _dbContextFactory;

    public TemplateRepository(IXcaNetDbContextFactory dbContextFactory)
    {
        _dbContextFactory = dbContextFactory;
    }

    public async Task<IReadOnlyList<TemplateEntity>> ListAsync(string databasePath, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.Templates
            .AsNoTracking()
            .OrderBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }
}
