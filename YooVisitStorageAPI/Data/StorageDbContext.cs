using Microsoft.EntityFrameworkCore;
using YooVisitStorageAPI.Models;

namespace YooVisitStorageAPI.Data;

public class StorageDbContext : DbContext
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options) { }

    // On dit à Entity Framework qu'il doit gérer une table "Photos"
    public DbSet<Photo> Photos { get; set; }
    public DbSet<UserApplication> Users { get; set; }
    public DbSet<PastilleRating> PastilleRatings { get; set; }
    public DbSet<Zone> Zones { get; set; }
    public DbSet<Pastille> Pastilles { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<Pastille>()
            .HasMany(p => p.Photos) // Une Pastille a plusieurs Photos
            .WithOne(photo => photo.Pastille) // Une Photo a une seule Pastille
            .HasForeignKey(photo => photo.PastilleId); // La clé étrangère est PastilleId

        // Cette ligne dit à Entity Framework pour CE CONTEXTE :
        // "Je connais la table 'Users', mais ne la touche jamais quand tu crées des migrations."
        modelBuilder.Entity<UserApplication>().ToTable("Users", t => t.ExcludeFromMigrations());

        modelBuilder.Entity<PastilleRating>()
            .HasIndex(r => new { r.PastilleId, r.RaterUserId })
            .IsUnique();
    }
}