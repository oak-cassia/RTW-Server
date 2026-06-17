using Microsoft.EntityFrameworkCore;
using RTWWebServer.Data.Entities;

namespace RTWWebServer.Data;

public class GameDbContext(DbContextOptions<GameDbContext> options) : DbContext(options)
{
    public DbSet<User> Users { get; set; }
    public DbSet<PlayerCharacter> PlayerCharacters { get; set; }
    public DbSet<PlayerLobbyFurniture> PlayerLobbyFurniture { get; set; }

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

        modelBuilder.Entity<PlayerCharacter>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.CharacterMasterId)
                .IsRequired();

            entity.Property(e => e.Level)
                .IsRequired();

            entity.Property(e => e.CurrentExp)
                .IsRequired();

            entity.Property(e => e.ObtainedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // Foreign key relationships - navigation property 없이 직접 설정
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_user_id");

            entity.HasIndex(e => new { e.UserId, e.CharacterMasterId })
                .IsUnique()
                .HasDatabaseName("uk_user_character");
        });

        modelBuilder.Entity<PlayerLobbyFurniture>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Id)
                .ValueGeneratedOnAdd();

            entity.Property(e => e.UserId)
                .IsRequired();

            entity.Property(e => e.FurnitureMasterId)
                .IsRequired();

            entity.Property(e => e.PosX)
                .IsRequired();

            entity.Property(e => e.PosY)
                .IsRequired();

            entity.Property(e => e.Rotation)
                .IsRequired();

            entity.Property(e => e.UpdatedAt)
                .IsRequired()
                .HasColumnType("datetime")
                .HasDefaultValueSql("CURRENT_TIMESTAMP");

            // 한 방에 같은 가구를 여러 개 둘 수 있으므로 (UserId, FurnitureMasterId) 유니크 제약은 두지 않는다.
            // 레이아웃 조회/교체는 항상 UserId 기준이라 성능 인덱스만 둔다.
            // 인덱스명은 PlayerCharacter의 ix_user_id와 충돌하지 않도록 테이블별로 구분한다
            // (SQLite는 인덱스명이 DB 전역이라 동명 인덱스를 허용하지 않는다).
            entity.HasIndex(e => e.UserId)
                .HasDatabaseName("ix_lobby_furniture_user_id");
        });

        // AccountDbContext와 일관성을 위해 base.OnModelCreating를 마지막에 호출
        // 자식 클래스에서 정의한 구성이 우선 적용되도록 하기 위함
        base.OnModelCreating(modelBuilder);
    }
}