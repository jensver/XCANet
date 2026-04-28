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
            .OrderByDescending(x => x.IsFavorite)
            .ThenByDescending(x => x.IsEnabled)
            .ThenBy(x => x.Name)
            .ToListAsync(cancellationToken);
    }

    public async Task<TemplateEntity?> GetAsync(string databasePath, Guid templateId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        return await dbContext.Templates
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
    }

    public async Task<TemplateEntity> SaveAsync(string databasePath, TemplateEntity template, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.Templates.SingleOrDefaultAsync(x => x.Id == template.Id, cancellationToken);
        if (existing is null)
        {
            dbContext.Templates.Add(template);
        }
        else
        {
            existing.Name = template.Name;
            existing.Description = template.Description;
            existing.IsFavorite = template.IsFavorite;
            existing.IsEnabled = template.IsEnabled;
            existing.IntendedUsage = template.IntendedUsage;
            existing.SubjectDefault = template.SubjectDefault;
            existing.SubjectAlternativeNames = template.SubjectAlternativeNames;
            existing.KeyAlgorithm = template.KeyAlgorithm;
            existing.RsaKeySize = template.RsaKeySize;
            existing.Curve = template.Curve;
            existing.SignatureAlgorithm = template.SignatureAlgorithm;
            existing.ValidityDays = template.ValidityDays;
            existing.IsCertificateAuthority = template.IsCertificateAuthority;
            existing.PathLengthConstraint = template.PathLengthConstraint;
            existing.KeyUsages = template.KeyUsages;
            existing.EnhancedKeyUsages = template.EnhancedKeyUsages;
            template = existing;
        }

        await dbContext.SaveChangesAsync(cancellationToken);
        return template;
    }

    public async Task<TemplateEntity?> CloneAsync(string databasePath, Guid templateId, string? newName, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.Templates.SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
        if (existing is null)
        {
            return null;
        }

        var clone = new TemplateEntity
        {
            Id = Guid.NewGuid(),
            Name = string.IsNullOrWhiteSpace(newName) ? $"{existing.Name} Copy" : newName.Trim(),
            Description = existing.Description,
            IsFavorite = false,
            IsEnabled = existing.IsEnabled,
            IntendedUsage = existing.IntendedUsage,
            SubjectDefault = existing.SubjectDefault,
            SubjectAlternativeNames = existing.SubjectAlternativeNames,
            KeyAlgorithm = existing.KeyAlgorithm,
            RsaKeySize = existing.RsaKeySize,
            Curve = existing.Curve,
            SignatureAlgorithm = existing.SignatureAlgorithm,
            ValidityDays = existing.ValidityDays,
            IsCertificateAuthority = existing.IsCertificateAuthority,
            PathLengthConstraint = existing.PathLengthConstraint,
            KeyUsages = existing.KeyUsages,
            EnhancedKeyUsages = existing.EnhancedKeyUsages
        };

        dbContext.Templates.Add(clone);
        await dbContext.SaveChangesAsync(cancellationToken);
        return clone;
    }

    public async Task<bool> SetFavoriteAsync(string databasePath, Guid templateId, bool isFavorite, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.Templates.SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        existing.IsFavorite = isFavorite;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> SetEnabledAsync(string databasePath, Guid templateId, bool isEnabled, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.Templates.SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        existing.IsEnabled = isEnabled;
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task<bool> DeleteAsync(string databasePath, Guid templateId, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var existing = await dbContext.Templates.SingleOrDefaultAsync(x => x.Id == templateId, cancellationToken);
        if (existing is null)
        {
            return false;
        }

        dbContext.Templates.Remove(existing);
        await dbContext.SaveChangesAsync(cancellationToken);
        return true;
    }

    public async Task UpdateDisplayNameAsync(string databasePath, Guid templateId, string newName, CancellationToken cancellationToken)
    {
        await using var dbContext = _dbContextFactory.CreateDbContext(databasePath);
        var template = await dbContext.Templates.SingleAsync(x => x.Id == templateId, cancellationToken);
        template.Name = newName;
        await dbContext.SaveChangesAsync(cancellationToken);
    }
}
