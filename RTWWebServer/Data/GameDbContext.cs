using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<User>(entity =>
            {
                entity.ToTable("Users");
                
                entity.HasKey(e => e.Id);
                
                entity.Property(e => e.Id)
                    .HasColumnName("id")
                    .ValueGeneratedOnAdd();
                
                entity.Property(e => e.Guid)
                    .HasColumnName("guid")
                    .HasMaxLength(64);
                
                entity.Property(e => e.Email)
                    .HasColumnName("email")
                    .HasMaxLength(256);
                
                entity.Property(e => e.UserType)
                    .HasColumnName("user_type")
                    .IsRequired();
                
                entity.Property(e => e.Nickname)
                    .HasColumnName("nickname")
                    .HasMaxLength(16);
                
                entity.Property(e => e.CreatedAt)
                    .HasColumnName("created_at");
                
                entity.Property(e => e.UpdatedAt)
                    .HasColumnName("updated_at");
                
                // Unique indexes
                entity.HasIndex(e => e.Guid)
                    .IsUnique()
                    .HasDatabaseName("uk_guid");
                
                entity.HasIndex(e => e.Email)
                    .IsUnique()
                    .HasDatabaseName("uk_email");
                
                entity.HasIndex(e => e.Nickname)
                    .IsUnique()
                    .HasDatabaseName("uk_nickname");
            });
        
        base.OnModelCreating(modelBuilder);
    }
}