
using Archive.Api.Domain;
using Microsoft.EntityFrameworkCore;

namespace Archive.Api.Infrastructure;

public sealed class ArchiveDbContext : DbContext
{
    public ArchiveDbContext(
        DbContextOptions<ArchiveDbContext> options)
        : base(options)
    {
    }

    public DbSet<Document> Documents => Set<Document>();

    public DbSet<DocumentPart> DocumentParts =>
        Set<DocumentPart>();

    public DbSet<DocumentIndexValue> DocumentIndexValues =>
        Set<DocumentIndexValue>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Document>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.CabinetName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.Name)
                .IsRequired()
                .HasMaxLength(255);

            entity.HasIndex(x => new
            {
                x.CabinetName,
                x.StoredAt,
                x.Id
            });

            entity.HasMany(x => x.Parts)
                .WithOne(x => x.Document)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);

            entity.HasMany(x => x.IndexValues)
                .WithOne(x => x.Document)
                .HasForeignKey(x => x.DocumentId)
                .OnDelete(DeleteBehavior.Cascade);
        });

        modelBuilder.Entity<DocumentPart>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FileName)
                .IsRequired();

            entity.Property(x => x.RelativePath)
                .IsRequired();

            entity.Property(x => x.StorageKey)
                .IsRequired();

            entity.HasIndex(x => x.StorageKey)
                .IsUnique();
        });

        modelBuilder.Entity<DocumentIndexValue>(entity =>
        {
            entity.HasKey(x => x.Id);

            entity.Property(x => x.FieldName)
                .IsRequired()
                .HasMaxLength(100);

            entity.Property(x => x.NumberValue)
                .HasPrecision(18, 4);

            entity.HasIndex(x => new
            {
                x.DocumentId,
                x.FieldName
            }).IsUnique();
        });
    }
}
