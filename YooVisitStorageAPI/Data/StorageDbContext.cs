using Microsoft.EntityFrameworkCore;
using YooVisitStorageAPI.Models;

namespace YooVisitStorageAPI.Data;

public class StorageDbContext : DbContext
{
    public StorageDbContext(DbContextOptions<StorageDbContext> options) : base(options) { }

    // On dit à Entity Framework qu'il doit gérer une table "Photos"
    public DbSet<Photo> Photos { get; set; }
}