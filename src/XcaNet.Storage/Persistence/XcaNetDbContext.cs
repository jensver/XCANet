using Microsoft.EntityFrameworkCore;
using XcaNet.Storage.Persistence.Entities;

namespace XcaNet.Storage.Persistence;

public sealed class XcaNetDbContext : DbContext
{
    public const int CurrentSchemaVersion = 1;

    public XcaNetDbContext(DbContextOptions<XcaNetDbContext> options)
        : base(options)
    {
    }

    public DbSet<PrivateKeyEntity> PrivateKeys => Set<PrivateKeyEntity>();
    public DbSet<CertificateEntity> Certificates => Set<CertificateEntity>();
    public DbSet<CertificateRequestEntity> CertificateRequests => Set<CertificateRequestEntity>();
    public DbSet<CertificateRevocationListEntity> CertificateRevocationLists => Set<CertificateRevocationListEntity>();
    public DbSet<TemplateEntity> Templates => Set<TemplateEntity>();
    public DbSet<AuthorityEntity> Authorities => Set<AuthorityEntity>();
    public DbSet<TagEntity> Tags => Set<TagEntity>();
    public DbSet<CertificateTagEntity> CertificateTags => Set<CertificateTagEntity>();
    public DbSet<AuditEventEntity> AuditEvents => Set<AuditEventEntity>();
    public DbSet<AppSettingEntity> AppSettings => Set<AppSettingEntity>();
    public DbSet<DatabaseProfileEntity> DatabaseProfiles => Set<DatabaseProfileEntity>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<PrivateKeyEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Algorithm).HasMaxLength(64).IsRequired();
            entity.Property(x => x.PublicKeyFingerprint).HasMaxLength(256).IsRequired();
            entity.HasIndex(x => x.PublicKeyFingerprint);
        });

        modelBuilder.Entity<CertificateEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(400).IsRequired();
            entity.Property(x => x.Issuer).HasMaxLength(400).IsRequired();
            entity.Property(x => x.SerialNumber).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Sha1Thumbprint).HasMaxLength(64).IsRequired();
            entity.Property(x => x.Sha256Thumbprint).HasMaxLength(128).IsRequired();
            entity.Property(x => x.DataFormat).HasMaxLength(32).IsRequired();
            entity.Property(x => x.KeyAlgorithm).HasMaxLength(64).IsRequired();
            entity.HasIndex(x => x.SerialNumber);
            entity.HasIndex(x => x.Sha1Thumbprint);
            entity.HasIndex(x => x.Sha256Thumbprint);
            entity.HasIndex(x => x.Subject);
            entity.HasIndex(x => x.Issuer);
            entity.HasIndex(x => x.NotBeforeUtc);
            entity.HasIndex(x => x.NotAfterUtc);
            entity.HasIndex(x => x.RevocationState);
            entity.HasIndex(x => x.RevokedAtUtc);
            entity.HasIndex(x => x.IssuerCertificateId);
        });

        modelBuilder.Entity<CertificateRequestEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.Subject).HasMaxLength(400).IsRequired();
            entity.Property(x => x.DataFormat).HasMaxLength(32).IsRequired();
            entity.Property(x => x.KeyAlgorithm).HasMaxLength(64).IsRequired();
        });

        modelBuilder.Entity<CertificateRevocationListEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.IssuerDisplayName).HasMaxLength(200).IsRequired();
            entity.OwnsMany(x => x.RevokedEntries, owned =>
            {
                owned.ToTable("CertificateRevocationListEntries");
                owned.WithOwner().HasForeignKey("CertificateRevocationListId");
                owned.Property<int>("Id");
                owned.HasKey("Id");
                owned.Property(x => x.SerialNumber).HasMaxLength(128).IsRequired();
                owned.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
                owned.Property(x => x.Subject).HasMaxLength(400).IsRequired();
            });
        });

        modelBuilder.Entity<TemplateEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
            entity.HasIndex(x => x.Name);
        });

        modelBuilder.Entity<AuthorityEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(200).IsRequired();
        });

        modelBuilder.Entity<TagEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.Name).HasMaxLength(128).IsRequired();
            entity.HasIndex(x => x.Name).IsUnique();
        });

        modelBuilder.Entity<CertificateTagEntity>(entity =>
        {
            entity.HasKey(x => new { x.CertificateId, x.TagId });
            entity.HasOne(x => x.Certificate)
                .WithMany(x => x.CertificateTags)
                .HasForeignKey(x => x.CertificateId)
                .OnDelete(DeleteBehavior.Cascade);
            entity.HasOne(x => x.Tag)
                .WithMany(x => x.CertificateTags)
                .HasForeignKey(x => x.TagId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<AuditEventEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.EventType).HasMaxLength(128).IsRequired();
            entity.Property(x => x.Message).HasMaxLength(512).IsRequired();
            entity.Property(x => x.EntityType).HasMaxLength(128);
            entity.HasIndex(x => x.OccurredUtc);
            entity.HasIndex(x => x.EntityId);
        });

        modelBuilder.Entity<AppSettingEntity>(entity =>
        {
            entity.HasKey(x => x.Key);
            entity.Property(x => x.Key).HasMaxLength(128);
        });

        modelBuilder.Entity<DatabaseProfileEntity>(entity =>
        {
            entity.HasKey(x => x.Id);
            entity.Property(x => x.DisplayName).HasMaxLength(200).IsRequired();
            entity.Property(x => x.KdfAlgorithm).HasMaxLength(64).IsRequired();
            entity.Property(x => x.EncryptionAlgorithm).HasMaxLength(64).IsRequired();
        });
    }
}
