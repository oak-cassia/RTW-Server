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
            // 데이터베이스 제약조건 제거 - 모든 비즈니스 로직은 User 엔티티에서 관리

            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.AccountId)
                .IsRequired();

            entity.Property(e => e.Nickname)
                .HasMaxLength(16)
                .IsRequired();

            entity.Property(e => e.Level)
                .IsRequired();

            entity.Property(e => e.CurrentExp)
                .IsRequired();

            entity.Property(e => e.CurrentStamina)
                .IsRequired();

            entity.Property(e => e.MaxStamina)
                .IsRequired();

            entity.Property(e => e.LastStaminaRecharge)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.PremiumCurrency)
                .IsRequired();

            entity.Property(e => e.FreeCurrency)
                .IsRequired();

            entity.Property(e => e.MainCharacterId)
                .IsRequired();

            entity.Property(e => e.CreatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 인덱스 설정 - 성능을 위한 설정만 유지
            entity.HasIndex(e => e.AccountId)
                .IsUnique()
                .HasDatabaseName("uk_account_id");

            entity.HasIndex(e => e.Nickname)
                .IsUnique()
                .HasDatabaseName("uk_nickname");
        });

        // AccountDbContext와 일관성을 위해 base.OnModelCreating를 마지막에 호출
        // 자식 클래스에서 정의한 구성이 우선 적용되도록 하기 위함
        base.OnModelCreating(modelBuilder);
    }
}