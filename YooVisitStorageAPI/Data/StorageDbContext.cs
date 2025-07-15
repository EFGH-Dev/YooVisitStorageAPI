using Microsoft.EntityFrameworkCore;
using YooVisitStorageAPI.Models;

namespace YooVisitStorageAPI.Data;

public class StorageDbContext : DbContext
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options) { }

    // On dit à Entity Framework qu'il doit gérer une table "Photos"
    public DbSet<Photo> Photos { get; set; }
    public DbSet<UserApplication> Users { get; set; }
    public DbSet<PhotoRating> PhotoRatings { get; set; }
    public DbSet<Zone> Zones { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Cette ligne dit à Entity Framework pour CE CONTEXTE :
        // "Je connais la table 'Users', mais ne la touche jamais quand tu crées des migrations."
        modelBuilder.Entity<UserApplication>().ToTable("Users", t => t.ExcludeFromMigrations());

        modelBuilder.Entity<PhotoRating>()
            .HasIndex(r => new { r.PhotoId, r.RaterUserId })
            .IsUnique();
    }
}